using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Sync;
using Inventory.Dto.Products.Results;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Tests;

public class IdentityReconciliationTests
{
    [Theory]
    [InlineData("unused", true)]
    [InlineData("balance", false)]
    [InlineData("different", false)]
    [InlineData("sale", false)]
    [InlineData("tenant", false)]
    [InlineData("edited", false)]
    [InlineData("payment", false)]
    [InlineData("return", false)]
    [InlineData("other-operation", false)]
    public async Task Archives_only_confirmed_unused_identical_customer(string scenario, bool expected)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new PosLocalDbContext(new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var tenant = Guid.NewGuid();
        var local = new LocalCustomer { TenantId = tenant, Name = "Same" };
        var owner = new LocalCustomer { TenantId = tenant, Name = "Same", ServerId = Guid.NewGuid(), SyncStatus = "Done", CurrentBalance = 20 };
        var queue = new SyncQueueItem { TenantId = tenant, EntityName = "Customer", LocalEntityId = local.Id, PayloadJson = "{}" };
        if (scenario == "balance") local.CurrentBalance = 5;
        if (scenario == "different") local.Phone = "different";
        if (scenario == "tenant") owner.TenantId = Guid.NewGuid();
        if (scenario == "edited") local.ModifiedAtUtc = DateTime.UtcNow;
        db.AddRange(local, owner, queue);
        if (scenario == "sale") db.Add(new LocalSale { TenantId = tenant, CustomerLocalId = local.Id, LocalInvoiceNumber = "S1" });
        if (scenario == "payment") db.Add(new LocalCustomerTransaction { TenantId = tenant, CustomerLocalId = local.Id, Type = "Payment", Amount = 5 });
        if (scenario == "return")
        {
            var sale = new LocalSale { TenantId = tenant, LocalInvoiceNumber = "S1" };
            db.Add(sale);
            db.Add(new LocalReturn { TenantId = tenant, LocalSaleId = sale.Id, CustomerLocalId = local.Id, LocalReturnNumber = "R1", RefundMethod = Inventory.Dto.Enums.RefundMethod.Cash });
        }
        if (scenario == "other-operation") db.Add(new SyncQueueItem { TenantId = tenant, EntityName = "Customer", LocalEntityId = local.Id, Operation = "Update", PayloadJson = "{}" });
        await db.SaveChangesAsync();
        Assert.Equal(expected, await CustomerDuplicateReconciler.TryArchiveAsync(db, queue, local, owner, default));
        await db.SaveChangesAsync();
        Assert.Equal(expected, local.IsDeleted);
        Assert.Equal(20, owner.CurrentBalance);
        Assert.Null(local.ServerId);
        Assert.Equal(2, await db.Customers.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_catalog_product_is_linked_and_price_change_is_queued_once(bool changed)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new PosLocalDbContext(new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var tenant = Guid.NewGuid();
        var local = new LocalProduct { TenantId = tenant, CatalogProductId = Guid.NewGuid(), Name = "P", SalePrice = changed ? 12 : 10 };
        var server = new ProductResult { Id = Guid.NewGuid(), CatalogProductId = local.CatalogProductId.Value, SalePrice = 10, IsTracked = local.IsTracked, Status = local.Status };
        var queue = new SyncQueueItem { TenantId = tenant, EntityName = "Product", LocalEntityId = local.Id, PayloadJson = "{}", Status = "Conflict" };
        db.ProductCatalogs.Add(new LocalProductCatalog { Id = local.CatalogProductId.Value, Name = "P" });
        db.AddRange(local, queue);
        await db.SaveChangesAsync();
        Assert.False(await ProductIdentityReconciler.ApplyAsync(db, Guid.NewGuid(), queue, server, default));
        Assert.True(await ProductIdentityReconciler.ApplyAsync(db, tenant, queue, server, default));
        await db.SaveChangesAsync();
        Assert.False(await ProductIdentityReconciler.ApplyAsync(db, tenant, queue, server, default));
        Assert.Equal(server.Id, local.ServerId);
        Assert.Equal("Done", queue.Status);
        var updates = await db.SyncQueueItems.Where(x => x.Operation == "Update").ToListAsync();
        Assert.Equal(changed ? 1 : 0, updates.Count);
        if (changed) Assert.NotEqual(queue.ClientOperationId, updates[0].ClientOperationId);
    }
}
