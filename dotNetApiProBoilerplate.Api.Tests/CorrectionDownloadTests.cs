using Inventory.Dto.Sales.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Tests;

public sealed class CorrectionDownloadTests
{
    [Theory]
    [InlineData("unrelated-customer", true)]
    [InlineData("product", true)]
    [InlineData("stock", true)]
    [InlineData("unrelated-sale", true)]
    [InlineData("customer", false)]
    [InlineData("old-customer", false)]
    [InlineData("sale", false)]
    [InlineData("payment", false)]
    [InlineData("return", false)]
    [InlineData("other-sale-same-account", false)]
    public async Task Download_ignores_unrelated_conflicts_but_preserves_related_work(string pending, bool expected)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new PosLocalDbContext(new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var tenant = Guid.NewGuid();
        var old = new LocalCustomer { TenantId = tenant, ServerId = Guid.NewGuid(), Name = "Old", SyncStatus = SyncQueueStatus.Done };
        var next = new LocalCustomer { TenantId = tenant, ServerId = Guid.NewGuid(), Name = "New", CurrentBalance = 7, SyncStatus = SyncQueueStatus.Done };
        var sale = new LocalSale { TenantId = tenant, ServerId = Guid.NewGuid(), LocalInvoiceNumber = "Receipt",
            CustomerLocalId = old.Id, CustomerServerId = old.ServerId, SyncStatus = SyncQueueStatus.Done };
        db.AddRange(old, next, sale);
        var queue = new SyncQueueItem { TenantId = tenant, EntityName = "Customer", LocalEntityId = Guid.NewGuid(),
            Status = SyncQueueStatus.Conflict, PayloadJson = "{}" };
        switch (pending)
        {
            case "product": queue.EntityName = "Product"; break;
            case "stock": queue.EntityName = "StockMovement"; break;
            case "customer": queue.LocalEntityId = next.Id; break;
            case "old-customer": queue.LocalEntityId = old.Id; break;
            case "sale": queue.EntityName = "Sale"; queue.LocalEntityId = sale.Id; break;
            case "payment":
                var payment = new LocalCustomerTransaction { TenantId = tenant, CustomerLocalId = next.Id,
                    CustomerServerId = next.ServerId, Type = "Payment", Amount = 3, UploadRequired = true, SyncStatus = SyncQueueStatus.Pending };
                db.Add(payment); queue.EntityName = "CustomerTransaction"; queue.LocalEntityId = payment.Id; break;
            case "return":
                var returned = new LocalReturn { TenantId = tenant, LocalSaleId = sale.Id, LocalReturnNumber = "Return",
                    RefundMethod = Inventory.Dto.Enums.RefundMethod.Cash, SyncStatus = SyncQueueStatus.Pending };
                db.Add(returned); queue.EntityName = "Return"; queue.LocalEntityId = returned.Id; break;
            case "unrelated-sale":
            case "other-sale-same-account":
                var other = new LocalSale { TenantId = tenant, LocalInvoiceNumber = "Other",
                    CustomerLocalId = pending == "other-sale-same-account" ? next.Id : null, SyncStatus = SyncQueueStatus.Pending };
                db.Add(other); queue.EntityName = "Sale"; queue.LocalEntityId = other.Id; break;
        }
        db.Add(queue);
        await db.SaveChangesAsync();
        var snapshot = new SaleCustomerSyncResult {
            Sales = new() { new() { SaleId = sale.ServerId!.Value, CustomerId = next.ServerId } },
            Customers = new() { new() { CustomerId = next.ServerId!.Value, Name = "New", Balance = 20 } }
        };
        Assert.Equal(expected, await new SaleCustomerCorrectionApplier(db).ApplyAsync(tenant, snapshot));
        Assert.Equal(expected ? next.Id : old.Id, sale.CustomerLocalId);
        Assert.Equal(expected ? 20 : 7, next.CurrentBalance);
        Assert.Equal(SyncQueueStatus.Conflict, (await db.SyncQueueItems.SingleAsync()).Status);
        Assert.Equal(0, await db.CustomerTransactions.CountAsync(x => x.Origin == "ServerCorrection"));
    }
}
