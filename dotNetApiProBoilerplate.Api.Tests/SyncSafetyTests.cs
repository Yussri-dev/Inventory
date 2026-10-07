using System.Reflection;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Stock.Results;
using Inventory.Dto.Sync;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Inventory.Api.Tests;

public class SyncSafetyTests
{
    public class SnapshotProxy : DispatchProxy
    {
        public object Result = null!;
        public Action? BeforeResponse;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            Assert.Equal("DownloadSnapshot", method!.Name); // No offset search pages.
            BeforeResponse?.Invoke();
            return Result;
        }
        public static T Create<T, TItem>(SyncDownload<TItem> snapshot, Action? before = null) where T : class
        {
            var proxy = Create<T, SnapshotProxy>();
            ((SnapshotProxy)(object)proxy).Result = Task.FromResult(snapshot);
            ((SnapshotProxy)(object)proxy).BeforeResponse = before;
            return proxy;
        }
    }

    [Theory]
    [InlineData("Sale", "Pending", true)]
    [InlineData("Purchase", "Conflict", true)]
    [InlineData("Return", "Failed", true)]
    [InlineData("Sale", "Done", false)]
    public async Task Stock_download_preserves_parent_operations(string entity, string status, bool protect)
    {
        using var cn = new SqliteConnection("Data Source=:memory:"); await cn.OpenAsync();
        await using var db = new PosLocalDbContext(new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(cn).Options);
        await db.Database.EnsureCreatedAsync();
        var tenant = new LocalTenantContext(); tenant.SetTenant(Guid.NewGuid()); var id = tenant.GetRequiredTenantId();
        var product = new LocalProduct { TenantId = id, ServerId = Guid.NewGuid(), Name = "Unit", IsTracked = true };
        var stock = new LocalStock { TenantId = id, ProductLocalId = product.Id, ProductServerId = product.ServerId,
            ServerId = Guid.NewGuid(), Quantity = 8, LastUpdatedUtc = DateTime.UtcNow.AddDays(-1) };
        var operationId = Guid.NewGuid();
        db.AddRange(product, stock, new LocalStockMovement { TenantId = id, ProductLocalId = product.Id,
            LocalReferenceId = operationId, QuantityChange = -2 }, new SyncQueueItem { TenantId = id,
            LocalEntityId = operationId, EntityName = entity, Operation = "Create", Status = status, PayloadJson = "{}" });
        await db.SaveChangesAsync();
        var api = SnapshotProxy.Create<IStockApi, StockResult>(new() { Items = new() {
            new StockResult { Id = stock.ServerId.Value, ProductId = product.ServerId!.Value, Quantity = 10, Name = "Unit" } } });
        await new LocalStockSyncService(db, api, tenant, NullLogger<LocalStockSyncService>.Instance).FullSyncAsync();
        Assert.Equal(protect ? 8 : 10, (await db.Stocks.SingleAsync()).Quantity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Customer_tombstone_preserves_pending_edits_and_other_store(bool pending)
    {
        using var cn = new SqliteConnection("Data Source=:memory:"); await cn.OpenAsync();
        await using var db = new PosLocalDbContext(new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(cn).Options);
        await db.Database.EnsureCreatedAsync();
        var tenant = new LocalTenantContext(); tenant.SetTenant(Guid.NewGuid()); var id = tenant.GetRequiredTenantId();
        var customer = new LocalCustomer { TenantId = id, Name = "Keep history", ServerId = Guid.NewGuid() };
        var other = new LocalCustomer { TenantId = Guid.NewGuid(), Name = "Other store", ServerId = Guid.NewGuid() };
        db.AddRange(customer, other);
        if (pending) db.Add(new SyncQueueItem { TenantId = id, LocalEntityId = customer.Id, EntityName = "Customer", Operation = "Update", Status = "Pending", PayloadJson = "{}" });
        await db.SaveChangesAsync();
        var api = SnapshotProxy.Create<ICustomerApi, CustomerResult>(new() { DeletedIds = new() { customer.ServerId.Value, other.ServerId.Value } });
        await new LocalCustomerSyncService(db, api, tenant, NullLogger<LocalCustomerSyncService>.Instance).FullSyncAsync();
        Assert.Equal(!pending, customer.IsDeleted); Assert.False(other.IsDeleted);
        Assert.Equal(2, await db.Customers.CountAsync()); // Never physically delete history.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Product_tombstone_preserves_pending_sale(bool pending)
    {
        using var cn = new SqliteConnection("Data Source=:memory:"); await cn.OpenAsync();
        await using var db = new PosLocalDbContext(new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(cn).Options);
        await db.Database.EnsureCreatedAsync();
        var tenant = new LocalTenantContext(); tenant.SetTenant(Guid.NewGuid()); var id = tenant.GetRequiredTenantId();
        var product = new LocalProduct { TenantId = id, Name = "Sold product", ServerId = Guid.NewGuid() };
        db.Add(product);
        if (pending)
        {
            var operation = Guid.NewGuid();
            db.AddRange(new SyncQueueItem { TenantId = id, EntityName = "Sale", LocalEntityId = operation, Operation = "Create", Status = "Pending", PayloadJson = "{}" },
                new LocalStockMovement { TenantId = id, ProductLocalId = product.Id, LocalReferenceId = operation, QuantityChange = -1 });
        }
        await db.SaveChangesAsync();
        var api = SnapshotProxy.Create<IProductApi, ProductResult>(new() { DeletedIds = new() { product.ServerId.Value } });
        await new LocalProductSyncService(db, api, tenant, NullLogger<LocalProductSyncService>.Instance).FullSyncAsync();
        Assert.Equal(!pending, product.IsDeletedLocally);
        Assert.Single(await db.Products.ToListAsync());
    }

    [Fact]
    public void Truncated_protocol_response_is_rejected()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => System.Text.Json.JsonSerializer.Deserialize<SyncDownload<CustomerResult>>("{}"));
    }
}
