using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Inventory.Api.Controllers;
using Inventory.Api.Installers;
using Inventory.Api.Middleware;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Enums;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.Infrastructure.Data;
using Inventory.Services;
using Inventory.Services.Abstractions;
using Inventory.Services.Handlers;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Inventory.Api.Tests;

public sealed partial class PostgreSqlSyncTests : IClassFixture<PostgreSqlSyncFixture>
{
    private readonly PostgreSqlSyncFixture fixture;
    public PostgreSqlSyncTests(PostgreSqlSyncFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task Missing_token_is_rejected()
    {
        using var response = await fixture.Client.PostAsJsonAsync("/api/sync/batches", new SyncBatchRequest { BatchId=Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }

    [Fact]
    public async Task Lost_response_then_resend_creates_sale_and_financial_effects_once()
    {
        var data=await fixture.Seed();
        var request=data.Request();
        var first=await fixture.Upload(data,request);
        Assert.Equal(SyncBatchItemStatus.Done,Assert.Single(first.Items).Status);
        // Client discards the committed response and resends the unchanged operation in a new batch.
        request.BatchId=Guid.NewGuid();
        request.Operations[0].QueueItemId=Guid.NewGuid();
        var retry=await fixture.Upload(data,request);
        var item=Assert.Single(retry.Items);
        Assert.Equal(SyncBatchItemStatus.Duplicate,item.Status);
        Assert.Equal(first.Items[0].ServerEntityId,item.ServerEntityId);
        Assert.Equal(first.Items[0].ServerReferenceNumber,item.ServerReferenceNumber);
        await fixture.AssertSaleEffects(data,1,8);
    }

    [Fact]
    public async Task Same_operation_with_changed_payload_is_conflict()
    {
        var data=await fixture.Seed();
        var request=data.Request();
        var first=await fixture.Upload(data,request);
        Assert.Equal(SyncBatchItemStatus.Done,first.Items.Single().Status);
        var payload=data.Sale();
        payload.Notes="Different content";
        request.Operations[0].Payload=JsonSerializer.SerializeToElement(payload);
        var second=await fixture.Upload(data,request);
        Assert.Equal(SyncBatchItemStatus.Conflict,second.Items.Single().Status);
        await fixture.AssertSaleEffects(data,1,8);
    }

    [Fact]
    public async Task Concurrent_resends_have_only_one_business_effect()
    {
        var data=await fixture.Seed();
        var request=data.Request();
        var results=await Task.WhenAll(Enumerable.Range(0,5).Select(_=>fixture.Upload(data,request)));
        Assert.Single(results,x=>x.Items.Single().Status==SyncBatchItemStatus.Done);
        Assert.Equal(4,results.Count(x=>x.Items.Single().Status==SyncBatchItemStatus.Duplicate));
        await fixture.AssertSaleEffects(data,1,8);
    }

    [Fact]
    public async Task Invalid_sale_rolls_back_business_writes()
    {
        var data=await fixture.Seed();
        var request=data.Request();
        var payload=data.Sale();
        payload.Lines.Add(new SaleLineItem { ProductId=Guid.NewGuid(),Quantity=1,UnitPrice=10 });
        payload.Payments![0].Amount=30;
        request.Operations[0].Payload=JsonSerializer.SerializeToElement(payload);
        var result=await fixture.Upload(data,request);
        Assert.Equal(SyncBatchItemStatus.Conflict,result.Items.Single().Status);
        await fixture.AssertSaleEffects(data,0,10);
        var retry=await fixture.Upload(data,request);
        Assert.Equal(SyncBatchItemStatus.Conflict,retry.Items.Single().Status);
        await fixture.AssertSaleEffects(data,0,10);
        await using var db=fixture.Database();
        Assert.Empty(await db.Set<DocumentNumber>().Where(x=>x.TenantId==data.TenantId).ToListAsync());
        Assert.Single(await db.SyncOperationRecords.Where(x=>x.TenantId==data.TenantId && x.Status==SyncBatchItemStatus.Conflict).ToListAsync());
    }

    [Fact]
    public async Task Transient_failure_after_business_writes_rolls_back_and_same_operation_can_retry()
    {
        var data=await fixture.Seed();
        var request=data.Request();
        fixture.FailOnce.TryAdd(request.Operations[0].ClientOperationId,true);
        var failed=await fixture.Upload(data,request);
        Assert.Equal(SyncBatchItemStatus.Failed,failed.Items.Single().Status);
        await fixture.AssertSaleEffects(data,0,10);
        await using(var db=fixture.Database())
            Assert.Empty(await db.SyncOperationRecords.Where(x=>x.TenantId==data.TenantId).ToListAsync());
        var retry=await fixture.Upload(data,request);
        Assert.Equal(SyncBatchItemStatus.Done,retry.Items.Single().Status);
        await fixture.AssertSaleEffects(data,1,8);
    }

    [Fact]
    public async Task Customer_batch_replay_is_idempotent()
    {
        var data=await fixture.Seed();
        var request=data.Request();
        request.Operations[0].EntityName="Customer";
        request.Operations[0].Payload=JsonSerializer.SerializeToElement(new Inventory.Dto.Customers.Requests.CreateCustomerRequest {
            Name="Offline customer",IsActive=true
        });
        var first=await fixture.Upload(data,request);
        Assert.Equal(SyncBatchItemStatus.Done,first.Items.Single().Status);
        var retry=await fixture.Upload(data,request);
        Assert.Equal(SyncBatchItemStatus.Duplicate,retry.Items.Single().Status);
        Assert.Equal(first.Items[0].ServerEntityId,retry.Items[0].ServerEntityId);
        await using var db=fixture.Database();
        Assert.Single(await db.Customers.Where(x=>x.TenantId==data.TenantId).ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Another_company_cannot_use_foreign_cash_session_or_product(bool foreignSession)
    {
        var a=await fixture.Seed();
        var b=await fixture.Seed();
        var payload=b.Sale();
        if(foreignSession) payload.CashSessionId=a.CashId;
        else payload.Lines[0].ProductId=a.ProductId;
        var request=b.Request();
        request.Operations[0].Payload=JsonSerializer.SerializeToElement(payload);
        var result=await fixture.Upload(b,request);
        Assert.Equal(SyncBatchItemStatus.Conflict,result.Items.Single().Status);
        await fixture.AssertSaleEffects(a,0,10);
        await fixture.AssertSaleEffects(b,0,10);
    }

    [Fact]
    public async Task Same_operation_id_in_two_companies_is_not_a_cross_company_duplicate()
    {
        var a=await fixture.Seed();
        var b=await fixture.Seed();
        var first=a.Request();
        var second=b.Request();
        second.Operations[0].ClientOperationId=first.Operations[0].ClientOperationId;
        var ar=await fixture.Upload(a,first);
        var br=await fixture.Upload(b,second);
        Assert.Equal(SyncBatchItemStatus.Done,ar.Items.Single().Status);
        Assert.Equal(SyncBatchItemStatus.Done,br.Items.Single().Status);
        Assert.NotEqual(ar.Items[0].ServerEntityId,br.Items[0].ServerEntityId);
        await fixture.AssertSaleEffects(a,1,8);
        await fixture.AssertSaleEffects(b,1,8);
    }
}

public sealed class PostgreSqlSyncFixture : IAsyncLifetime
{
    private string bin="";
    private string directory="";
    private bool started;
    private string connection="";
    private WebApplication? app;
    private readonly byte[] key=RandomNumberGenerator.GetBytes(64);
    public HttpClient Client { get; private set; }=null!;
    public ConcurrentDictionary<Guid,bool> FailOnce { get; }=new();
    public ConcurrentQueue<Exception> Errors { get; } = new();

    public async Task InitializeAsync()
    {
        bin=Environment.GetEnvironmentVariable("INVENTORY_TEST_PG_BIN")
            ?? @"C:\Program Files\PostgreSQL\17\bin";
        if(!File.Exists(Path.Combine(bin,"initdb.exe")))
            throw new InvalidOperationException("Set INVENTORY_TEST_PG_BIN to a PostgreSQL binaries directory.");
        directory=Path.Combine(Path.GetTempPath(),"InventoryPgTests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var listener=new TcpListener(IPAddress.Loopback,0);
        listener.Start();
        int port=((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        try {
            await Pg("initdb.exe","-D",Path.Combine(directory,"data"),"-U","inventory_test","-A","trust","--encoding=UTF8","--locale=C");
            await Pg("pg_ctl.exe","-D",Path.Combine(directory,"data"),"-l",Path.Combine(directory,"postgres.log"),
                "-o",$"-h 127.0.0.1 -p {port}","-w","start");
            started=true;
            connection=$"Host=127.0.0.1;Port={port};Database=postgres;Username=inventory_test;Pooling=false;Include Error Detail=true";
            await using(var db=Database()) await db.Database.EnsureCreatedAsync();

            var builder=WebApplication.CreateBuilder(new WebApplicationOptions {
                EnvironmentName="Production",ApplicationName=typeof(SyncBatchController).Assembly.FullName
            });
            builder.Configuration["Jwt:Key"]=Convert.ToBase64String(key);
            builder.Configuration["Jwt:Issuer"]="inventory-tests";
            builder.Configuration["Jwt:Audience"]="inventory-tests";
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddDbContext<InventoryDbContext>(o=>o.UseNpgsql(connection));
            builder.InstallServices().InstallMapping().InstallAuthentication().InstallVersioning().InstallMediatR();
            var saleRegistration=builder.Services.Single(d=>d.ServiceType==typeof(ISyncOperationHandler) &&
                d.ImplementationType==typeof(SaleSyncOperationHandler));
            builder.Services.Remove(saleRegistration);
            builder.Services.AddScoped<ISyncOperationHandler>(sp=>new FaultAfterSaleHandler(
                new SaleSyncOperationHandler(sp.GetRequiredService<SaleService>()),FailOnce));
            builder.Services.AddAuthorization();
            builder.Services.AddControllers().AddApplicationPart(typeof(SyncBatchController).Assembly);
            app=builder.Build();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.Use(async (context, next) => {
                try { await next(context); }
                catch (Exception error) { Errors.Enqueue(error); throw; }
            });
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();
            Client=app.GetTestClient();
        } catch { await DisposeAsync(); throw; }
    }

    private async Task Pg(string executable,params string[] arguments)
    {
        var start=new ProcessStartInfo(Path.Combine(bin,executable)) {
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true
        };
        foreach(var argument in arguments) start.ArgumentList.Add(argument);
        using var process=Process.Start(start)!;
        var stdout=process.StandardOutput.ReadToEndAsync();
        var stderr=process.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(true); throw; }
        if(process.ExitCode!=0) throw new InvalidOperationException(executable+": "+await stdout+await stderr);
    }

    public InventoryDbContext Database()=>new(
        new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(connection).Options,new AdminAccess());

    private sealed class AdminAccess : ITenantDataAccess {
        public bool IsAuthenticated=>true; public bool IsSuperAdmin=>true; public Guid TenantId=>Guid.Empty;
    }

    public async Task<SeedData> Seed()
    {
        var data=new SeedData();
        await using var db=Database();
        db.Tenants.Add(new Tenant { Id=data.TenantId,Name="Test tenant" });
        db.Users.Add(new ApplicationUser {Id=data.UserId,TenantId=data.TenantId,UserName=data.UserId.ToString()});
        var category=new ProductCategory {Id=Guid.NewGuid(),Name="Test category"};
        db.Add(category);
        var catalog=new ProductCatalog {Id=Guid.NewGuid(),Name="Test product",InternalCode=Guid.NewGuid().ToString("N"),CategoryId=category.Id};
        db.Add(catalog);
        db.Products.Add(new Product {Id=data.ProductId,TenantId=data.TenantId,CatalogProductId=catalog.Id,
            Name="Test product",Sku=catalog.InternalCode,IsTracked=true,SalePrice=10,IsActive=Inventory.Domain.Enums.ProductStatus.Active});
        db.Stocks.Add(new Stock {Id=Guid.NewGuid(),TenantId=data.TenantId,ProductId=data.ProductId,Quantity=10,LastUpdated=DateTime.UtcNow});
        db.CashSessions.Add(new CashSession {Id=data.CashId,TenantId=data.TenantId,ClientOperationId=Guid.NewGuid(),
            SessionNumber="TEST",OpenedAt=DateTime.UtcNow,OpenedByUserId=data.UserId,Status=CashSessionStatus.Open});
        foreach(var entry in db.ChangeTracker.Entries())
            foreach(var property in entry.Properties) {
                if(property.Metadata.ClrType==typeof(string) && !property.Metadata.IsNullable && property.CurrentValue==null)
                    property.CurrentValue="Test-"+Guid.NewGuid().ToString("N");
                if(property.CurrentValue is DateTime date && date.Kind!=DateTimeKind.Utc)
                    property.CurrentValue=DateTime.SpecifyKind(date,DateTimeKind.Utc);
            }
        await db.SaveChangesAsync();
        return data;
    }

    public async Task<SyncBatchResult> Upload(SeedData data,SyncBatchRequest request)
    {
        using var response=await Send(data,"/api/sync/batches",request);
        var text=await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode,$"HTTP {(int)response.StatusCode}: {text}");
        return JsonSerializer.Deserialize<SyncBatchResult>(text,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    public async Task<HttpResponseMessage> Send(SeedData data,string path,object? request, string role = "Cashier", HttpMethod? method = null)
    {
        var token=new JwtSecurityToken("inventory-tests","inventory-tests",new[] {
            new Claim(ClaimTypes.NameIdentifier,data.UserId.ToString()),new Claim("TenantId",data.TenantId.ToString()),
            new Claim(ClaimTypes.Role,role)
        },expires:DateTime.UtcNow.AddMinutes(10),signingCredentials:new SigningCredentials(new SymmetricSecurityKey(key),SecurityAlgorithms.HmacSha256));
        using var message=new HttpRequestMessage(method ?? HttpMethod.Post,path) {Content=request == null ? null : JsonContent.Create(request)};
        message.Headers.Authorization=new AuthenticationHeaderValue("Bearer",new JwtSecurityTokenHandler().WriteToken(token));
        return await Client.SendAsync(message);
    }

    public async Task AssertSaleEffects(SeedData data,int expected,decimal stock)
    {
        await using var db=Database();
        Assert.Equal(expected,await db.Sales.CountAsync(x=>x.TenantId==data.TenantId));
        Assert.Equal(expected,await db.SaleLines.CountAsync(x=>x.TenantId==data.TenantId));
        Assert.Equal(expected,await db.Payments.CountAsync(x=>x.TenantId==data.TenantId));
        Assert.Equal(expected,await db.StockMovements.CountAsync(x=>x.TenantId==data.TenantId));
        Assert.Equal(expected,await db.CashMovements.CountAsync(x=>x.TenantId==data.TenantId));
        Assert.Equal(stock,(await db.Stocks.SingleAsync(x=>x.TenantId==data.TenantId)).Quantity);
        if(expected==1) {
            Assert.Equal(20,(await db.Sales.SingleAsync(x=>x.TenantId==data.TenantId)).TotalAmount);
            Assert.Equal(20,(await db.Payments.SingleAsync(x=>x.TenantId==data.TenantId)).Amount);
            Assert.Equal(20,(await db.CashMovements.SingleAsync(x=>x.TenantId==data.TenantId)).Amount);
            Assert.Single(await db.SyncOperationRecords.Where(x=>x.TenantId==data.TenantId && x.Status==SyncBatchItemStatus.Done).ToListAsync());
        }
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if(app!=null){await app.DisposeAsync();app=null;}
        if(started) {
            await Pg("pg_ctl.exe","-D",Path.Combine(directory,"data"),"-m","fast","-w","stop");
            started=false;
        }
        // Keep isolated files and PostgreSQL logs for diagnosis. No existing database is touched.
    }
}

// Fault injection only: the real sale handler and persistence execute first.
internal sealed class FaultAfterSaleHandler(ISyncOperationHandler inner,ConcurrentDictionary<Guid,bool> failOnce) : ISyncOperationHandler
{
    public string EntityName=>inner.EntityName;
    public async Task<SyncBatchItemResult> ProcessAsync(SyncBatchOperationRequest operation,CancellationToken cancellationToken=default)
    {
        var result=await inner.ProcessAsync(operation,cancellationToken);
        if(failOnce.TryRemove(operation.ClientOperationId,out _))
            throw new TimeoutException("Injected interruption after sale writes, before transaction commit.");
        return result;
    }
}

public sealed class SeedData
{
    public Guid TenantId {get;}=Guid.NewGuid();
    public Guid UserId {get;}=Guid.NewGuid();
    public Guid ProductId {get;}=Guid.NewGuid();
    public Guid CashId {get;}=Guid.NewGuid();
    public CreateCompleteSaleRequest Sale()=>new() {CashSessionId=CashId,SaleDate=DateTime.UtcNow,
        Lines=new(){new(){ProductId=ProductId,Quantity=2,UnitPrice=10}},
        Payments=new(){new(){Amount=20,PaymentMethod="Cash"}}};
    public SyncBatchRequest Request()=>new() {BatchId=Guid.NewGuid(),Operations=new(){new(){
        QueueItemId=Guid.NewGuid(),ClientOperationId=Guid.NewGuid(),LocalEntityId=Guid.NewGuid(),
        EntityName="Sale",Operation="Create",CreatedAtUtc=DateTime.UtcNow,Payload=JsonSerializer.SerializeToElement(Sale())
    }}};
}
