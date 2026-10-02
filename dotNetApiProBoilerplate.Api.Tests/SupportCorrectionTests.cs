using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inventory.Domain.Entities;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Inventory.Api.Tests;

public sealed partial class PostgreSqlSyncTests
{
    private async Task<(Guid SaleId, Guid OldId, Guid NewId, CorrectSaleCustomerRequest Request)> Correction(SeedData data, bool credit = false)
    {
        var oldId = await Customer(data);
        var newId = await Customer(data);
        var batch = data.Request();
        var payload = data.Sale();
        payload.CustomerId = oldId;
        if (credit) payload.Payments[0].PaymentMethod = "Credit";
        batch.Operations[0].Payload = JsonSerializer.SerializeToElement(payload);
        Assert.Equal("Done", (await fixture.Upload(data, batch)).Items.Single().Status);
        await using var db = fixture.Database();
        var sale = await db.Sales.SingleAsync(x => x.TenantId == data.TenantId);
        return (sale.Id, oldId, newId, new CorrectSaleCustomerRequest { OperationId = Guid.NewGuid(), CustomerId = newId,
            ExpectedCustomerId = oldId, ExpectedModifiedAt = sale.ModifiedAt, ExpectedDebt = credit ? 20 : 0, Reason = "Mauvais client à la caisse" });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Support_correction_is_atomic_audited_and_replayable(bool credit)
    {
        var data = await fixture.Seed();
        var c = await Correction(data, credit);
        var path = $"/api/support/{data.TenantId}/sales/{c.SaleId}/customer";
        using var listing = await fixture.Send(data, $"/api/support/{data.TenantId}/sales", null, "Admin", HttpMethod.Get);
        Assert.True(listing.IsSuccessStatusCode, await listing.Content.ReadAsStringAsync());
        var listed = await listing.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(credit ? 20 : 0, listed[0].GetProperty("debtToTransfer").GetDecimal());
        using var first = await fixture.Send(data, path, c.Request, "Admin");
        Assert.True(first.IsSuccessStatusCode, await first.Content.ReadAsStringAsync());
        using var retry = await fixture.Send(data, path, c.Request, "Admin");
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        await using var db = fixture.Database();
        Assert.Equal(c.NewId, (await db.Sales.SingleAsync(x => x.Id == c.SaleId)).CustomerId);
        Assert.Equal(0, (await db.Customers.SingleAsync(x => x.Id == c.OldId)).CurrentBalance);
        Assert.Equal(credit ? 20 : 0, (await db.Customers.SingleAsync(x => x.Id == c.NewId)).CurrentBalance);
        var audit = Assert.Single(await db.Set<SaleCustomerCorrection>().Where(x => x.TenantId == data.TenantId).ToListAsync());
        Assert.Equal(data.UserId, audit.ActorUserId);
        Assert.Equal(c.OldId, audit.PreviousCustomerId);
        Assert.Equal(8, (await db.Stocks.SingleAsync(x => x.TenantId == data.TenantId)).Quantity);
        Assert.Single(await db.StockMovements.Where(x => x.TenantId == data.TenantId).ToListAsync());
        Assert.Equal(credit ? 0 : 1, await db.CashMovements.CountAsync(x => x.TenantId == data.TenantId));
        Assert.Equal(credit ? 3 : 0, await db.CustomerTransactions.CountAsync(x => x.TenantId == data.TenantId));
        using var history = await fixture.Send(data, $"/api/support/{data.TenantId}/corrections", null, "Admin", HttpMethod.Get);
        Assert.True(history.IsSuccessStatusCode, await history.Content.ReadAsStringAsync());
        Assert.Single((await history.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
        c.Request.Reason = "Autre opération";
        using var changed = await fixture.Send(data, path, c.Request, "Admin");
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
    }

    [Fact]
    public async Task Support_denies_cashier_foreign_store_and_foreign_customer()
    {
        var data = await fixture.Seed();
        var other = await fixture.Seed();
        var c = await Correction(data);
        var path = $"/api/support/{data.TenantId}/sales/{c.SaleId}/customer";
        using var cashier = await fixture.Send(data, path, c.Request);
        Assert.Equal(HttpStatusCode.Forbidden, cashier.StatusCode);
        using var foreign = await fixture.Send(other, path, c.Request, "Admin");
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        c.Request.CustomerId = await Customer(other);
        using var customer = await fixture.Send(data, path, c.Request, "Admin");
        Assert.Equal(HttpStatusCode.BadRequest, customer.StatusCode);
        await using var db = fixture.Database();
        Assert.Empty(await db.Set<SaleCustomerCorrection>().Where(x => x.TenantId == data.TenantId).ToListAsync());
    }

    [Fact]
    public async Task Support_superadmin_can_select_another_store_and_stale_screen_is_rejected()
    {
        var data = await fixture.Seed();
        var support = await fixture.Seed();
        var c = await Correction(data);
        using var stores = await fixture.Send(data, "/api/support/stores", null, "Admin", HttpMethod.Get);
        var visible = await stores.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(visible.EnumerateArray());
        using var applied = await fixture.Send(support, $"/api/support/{data.TenantId}/sales/{c.SaleId}/customer", c.Request, "SuperAdmin");
        Assert.True(applied.IsSuccessStatusCode, await applied.Content.ReadAsStringAsync());
        c.Request.OperationId = Guid.NewGuid();
        using var stale = await fixture.Send(data, $"/api/support/{data.TenantId}/sales/{c.SaleId}/customer", c.Request, "Admin");
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task Support_refuses_credit_with_later_payment_without_mutations()
    {
        var data = await fixture.Seed();
        var c = await Correction(data, true);
        await PostOk(data, "/api/v1/customertransactions/register-payment", new Inventory.Dto.CustomerTransactions.Requests.RegisterCustomerPaymentRequest {
            ClientOperationId = Guid.NewGuid(), CustomerId = c.OldId, Amount = 5, IsCash = false });
        using var response = await fixture.Send(data, $"/api/support/{data.TenantId}/sales/{c.SaleId}/customer", c.Request, "Admin");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = fixture.Database();
        Assert.Equal(c.OldId, (await db.Sales.SingleAsync(x => x.Id == c.SaleId)).CustomerId);
        Assert.Equal(15, (await db.Customers.SingleAsync(x => x.Id == c.OldId)).CurrentBalance);
        Assert.Equal(0, (await db.Customers.SingleAsync(x => x.Id == c.NewId)).CurrentBalance);
        Assert.Empty(await db.Set<SaleCustomerCorrection>().Where(x => x.TenantId == data.TenantId).ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Server_correction_reaches_sqlite_and_replay_does_not_duplicate_ledger(bool customerAlreadyLocal)
    {
        var data = await fixture.Seed();
        var c = await Correction(data, true);
        using var correction = await fixture.Send(data, $"/api/support/{data.TenantId}/sales/{c.SaleId}/customer", c.Request, "Admin");
        Assert.True(correction.IsSuccessStatusCode, await correction.Content.ReadAsStringAsync());
        using var download = await fixture.Send(data, "/api/sync/sale-customers", new[] { c.SaleId });
        var snapshot = (await download.Content.ReadFromJsonAsync<SaleCustomerSyncResult>())!;
        Assert.Equal(c.NewId, Assert.Single(snapshot.Sales).CustomerId);
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var local = new PosLocalDbContext(new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(connection).Options);
        await local.Database.EnsureCreatedAsync();
        var old = new LocalCustomer { TenantId = data.TenantId, ServerId = c.OldId, Name = "Ancien", CurrentBalance = 20, SyncStatus = SyncQueueStatus.Done };
        var next = new LocalCustomer { TenantId = data.TenantId, ServerId = c.NewId, Name = "Nouveau", SyncStatus = SyncQueueStatus.Done };
        var sale = new LocalSale { TenantId = data.TenantId, ServerId = c.SaleId, CustomerServerId = c.OldId, CustomerLocalId = old.Id,
            LocalInvoiceNumber = "TEST", SyncStatus = SyncQueueStatus.Done };
        local.AddRange(old, sale);
        if (customerAlreadyLocal) local.Add(next);
        await local.SaveChangesAsync();
        var applier = new SaleCustomerCorrectionApplier(local);
        Assert.True(await applier.ApplyAsync(data.TenantId, snapshot));
        Assert.True(await applier.ApplyAsync(data.TenantId, snapshot));
        next = await local.Customers.SingleAsync(x => x.ServerId == c.NewId);
        Assert.Equal(next.Id, sale.CustomerLocalId);
        Assert.Equal(0, old.CurrentBalance); Assert.Equal(20, next.CurrentBalance);
        Assert.Equal(2, await local.CustomerTransactions.CountAsync());
        Assert.All(await local.CustomerTransactions.ToListAsync(), x => Assert.False(x.UploadRequired));
        Assert.Empty(await local.SyncQueueItems.ToListAsync());
        // A pending offline edit must never be overwritten by the downloaded server snapshot.
        sale.SyncStatus = SyncQueueStatus.Pending; next.CurrentBalance = 30; await local.SaveChangesAsync();
        Assert.False(await applier.ApplyAsync(data.TenantId, snapshot));
        Assert.Equal(30, next.CurrentBalance);
        using var foreign = await fixture.Send(await fixture.Seed(), "/api/sync/sale-customers", new[] { c.SaleId });
        Assert.Empty((await foreign.Content.ReadFromJsonAsync<SaleCustomerSyncResult>())!.Sales);
    }

    [Fact]
    public async Task Correction_migration_creates_the_audit_table_on_postgresql()
    {
        await using var db = fixture.Database();
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Disposable test cluster only. Rollback restores the table and all its rows after the check.
        var entity = db.Model.FindEntityType(typeof(SaleCustomerCorrection))!;
        var table = db.GetService<Microsoft.EntityFrameworkCore.Storage.ISqlGenerationHelper>()
            .DelimitIdentifier(entity.GetTableName()!, entity.GetSchema());
        await db.Database.ExecuteSqlRawAsync("DROP TABLE " + table);
        var migration = new Inventory.Infrastructure.Migrations.AddSaleCustomerCorrections();
        var commands = db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations, db.Model);
        foreach (var command in commands) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        Assert.Empty(await db.Set<SaleCustomerCorrection>().ToListAsync());
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Concurrent_support_corrections_transfer_the_debt_only_once()
    {
        var data = await fixture.Seed();
        var c = await Correction(data, true);
        var path = $"/api/support/{data.TenantId}/sales/{c.SaleId}/customer";
        var responses = await Task.WhenAll(fixture.Send(data, path, c.Request, "Admin"), fixture.Send(data, path, c.Request, "Admin"));
        try
        {
            Assert.Contains(responses, x => x.StatusCode == HttpStatusCode.OK);
            Assert.All(responses, x => Assert.True(x.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
                string.Join("\n", fixture.Errors.Select(error => error.ToString()))));
        }
        finally { foreach (var response in responses) response.Dispose(); }
        await using var db = fixture.Database();
        Assert.Single(await db.Set<SaleCustomerCorrection>().Where(x => x.TenantId == data.TenantId).ToListAsync());
        Assert.Equal(20, (await db.Customers.SingleAsync(x => x.Id == c.NewId)).CurrentBalance);
        Assert.Equal(0, (await db.Customers.SingleAsync(x => x.Id == c.OldId)).CurrentBalance);
    }
}
