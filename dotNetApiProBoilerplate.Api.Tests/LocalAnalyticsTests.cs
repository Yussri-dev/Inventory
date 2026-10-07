using Inventory.Dto.Enums;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services;
using Inventory.Ui.Services.Analytics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Tests;

public class LocalAnalyticsTests
{
    [Theory]
    [InlineData(true, 2, 0)]
    [InlineData(false, 2, -60)]
    [InlineData(true, 1, 20)]
    public async Task Returns_reverse_cost_only_when_restocked(bool restock, int returnedQuantity, int expectedProfit)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new PosLocalDbContext(new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var tenant = new LocalTenantContext(); tenant.SetTenant(Guid.NewGuid()); var id = tenant.GetRequiredTenantId();
        var day = new DateOnly(2026, 10, 5);
        var at = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(new TimeOnly(12, 0)), TimeZoneInfo.Local);
        var product = new LocalProduct { TenantId = id, Name = "Returned item" };
        var sale = new LocalSale { TenantId = id, LocalInvoiceNumber = "S", SaleDateUtc = at, TotalAmount = 100 };
        var line = new LocalSaleLine { TenantId = id, LocalSaleId = sale.Id, ProductLocalId = product.Id,
            UnitProductLocalId = product.Id, Quantity = 2, UnitQuantity = 2, UnitCostPrice = 30, UnitPrice = 50 };
        var returned = new LocalReturn { TenantId = id, LocalSaleId = sale.Id, LocalReturnNumber = "R",
            ReturnDateUtc = at, IsProcessed = true, TotalAmount = 50 * returnedQuantity };
        db.AddRange(product, sale, line, returned, new LocalReturnLine { TenantId = id, LocalReturnId = returned.Id,
            LocalSaleLineId = line.Id, ProductLocalId = product.Id, UnitProductLocalId = product.Id,
            Quantity = returnedQuantity, UnitQuantity = returnedQuantity, UnitCostPrice = 30, RestockItem = restock });
        await db.SaveChangesAsync();
        var summary = await new LocalAnalyticsService(db, tenant).GetDashboardSummaryAsync(day, day);
        Assert.Equal(expectedProfit, summary.Profit);
        Assert.Equal(restock ? 60 - returnedQuantity * 30 : 60, summary.Cost);
    }

    [Fact]
    public async Task Dashboard_uses_unsynced_local_sales_and_isolates_store_and_period()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new PosLocalDbContext(new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var tenant = new LocalTenantContext();
        tenant.SetTenant(Guid.NewGuid());
        var id = tenant.GetRequiredTenantId();
        var day = new DateOnly(2026, 9, 26);
        var start = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local);
        var end = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local);
        var customer = new LocalCustomer { TenantId = id, Name = "Local customer" };
        var product = new LocalProduct { TenantId = id, Name = "Local product" };
        var sale = new LocalSale { TenantId = id, LocalInvoiceNumber = "LOCAL", SaleDateUtc = start,
            TotalAmount = 100, CustomerLocalId = customer.Id, SyncStatus = "Pending" };
        db.AddRange(customer, product, sale,
            new LocalSale { TenantId = Guid.NewGuid(), LocalInvoiceNumber = "OTHER", SaleDateUtc = start, TotalAmount = 999 },
            new LocalSale { TenantId = id, LocalInvoiceNumber = "DRAFT", SaleDateUtc = start, Status = SaleStatus.Draft, TotalAmount = 999 },
            new LocalSale { TenantId = id, LocalInvoiceNumber = "CANCELLED", SaleDateUtc = start, Status = SaleStatus.Cancelled, TotalAmount = 999 },
            new LocalSale { TenantId = id, LocalInvoiceNumber = "TOMORROW", SaleDateUtc = end, TotalAmount = 999 });
        db.Add(new LocalSaleLine { TenantId = id, LocalSaleId = sale.Id, ProductLocalId = product.Id,
            UnitProductLocalId = product.Id, ProductName = product.Name, Quantity = 2, UnitQuantity = 2,
            UnitPrice = 50, UnitCostPrice = 20 });
        db.Add(new LocalPayment { TenantId = id, LocalSaleId = sale.Id, Method = PaymentMethod.Cash, Amount = 100, PaidAtUtc = start });
        var returned = new LocalReturn { TenantId = id, LocalSaleId = sale.Id, LocalReturnNumber = "R", ReturnDateUtc = start,
            IsProcessed = true, TotalAmount = 10, RefundMethod = RefundMethod.Cash };
        db.Add(returned);
        db.Add(new LocalStockMovement { TenantId = id, ProductLocalId = product.Id, ProductName = product.Name,
            Type = StockMovementType.Damage, QuantityChange = -1, UnitCost = 5, MovementDateUtc = start });
        await db.SaveChangesAsync();
        var service = new LocalAnalyticsService(db, tenant);
        var summary = await service.GetDashboardSummaryAsync(day, day);
        Assert.Equal(100, summary.Revenue);
        Assert.Equal(40, summary.Cost);
        Assert.Equal(10, summary.Refunds);
        Assert.Equal(45, summary.Profit);
        Assert.Equal(100, summary.CashRevenue);
        Assert.Equal(1, summary.SalesCount);
        Assert.Equal("Local customer", Assert.Single(summary.RecentSales).CustomerName);
        Assert.Equal(100, Assert.Single(summary.TopProducts).TotalRevenue);
        Assert.Equal(100, Assert.Single(await service.GetDailyRevenueAsync(day, day)).Value);
        Assert.Equal(1, (await service.GetWeeklyAsync(day, day)).ReturnsCount);
        Assert.Equal(45, (await service.GetProfitAsync(day, day)).GrossProfit);
        Assert.Equal(5, Assert.Single(await service.GetLossProductsAsync(day, day)).LostRevenue);
        tenant.SetTenant(Guid.NewGuid());
        Assert.Equal(0, (await service.GetDashboardSummaryAsync(day, day)).SalesCount);
        tenant.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDashboardSummaryAsync(day, day));
    }
}
