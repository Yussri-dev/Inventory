using Inventory.Dto.Analytics.Results;
using Inventory.Dto.Enums;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Ui.Services.Analytics;

public class LocalAnalyticsService(PosLocalDbContext db, ILocalTenantContext tenant) : ILocalAnalyticsService
{
    private static (DateTime Start, DateTime End) Bounds(DateOnly from, DateOnly to)
    {
        if (to < from) throw new ArgumentException("The end date cannot precede the start date.");
        return (TimeZoneInfo.ConvertTimeToUtc(from.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local),
            TimeZoneInfo.ConvertTimeToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local));
    }

    private IQueryable<LocalSale> Sales(Guid id, DateTime start, DateTime end) => db.Sales.AsNoTracking()
        .Where(x => x.TenantId == id && x.SaleDateUtc >= start && x.SaleDateUtc < end &&
            (x.Status == SaleStatus.Completed || x.Status == SaleStatus.PartiallyPaid || x.Status == SaleStatus.Refunded));

    public async Task<DashboardSummaryResult> GetDashboardSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var id = tenant.GetRequiredTenantId();
        var (start, end) = Bounds(from, to);
        // No SyncStatus filter: offline sales belong in today's totals immediately.
        var sales = await Sales(id, start, end).Include(x => x.Lines.Where(l => l.TenantId == id))
            .Include(x => x.Payments.Where(p => p.TenantId == id)).OrderByDescending(x => x.SaleDateUtc).ToListAsync(cancellationToken);
        var returns = await db.Returns.AsNoTracking().Where(x => x.TenantId == id && x.IsProcessed &&
            x.ReturnDateUtc >= start && x.ReturnDateUtc < end).ToListAsync(cancellationToken);
        var damages = await db.StockMovements.AsNoTracking().Where(x => x.TenantId == id &&
            x.Type == StockMovementType.Damage && x.MovementDateUtc >= start && x.MovementDateUtc < end)
            .Select(x => new { x.QuantityChange, x.UnitCost }).ToListAsync(cancellationToken);
        var customerIds = sales.Where(x => x.CustomerLocalId.HasValue).Select(x => x.CustomerLocalId!.Value).Distinct().ToList();
        var customers = await db.Customers.AsNoTracking().Where(x => x.TenantId == id && customerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var revenue = sales.Sum(x => x.TotalAmount);
        var refunds = returns.Sum(x => x.TotalAmount);
        var cost = sales.SelectMany(x => x.Lines).Sum(x => x.UnitCostPrice * (x.UnitQuantity > 0 ? x.UnitQuantity : x.Quantity));
        var restocked = await db.ReturnLines.AsNoTracking().Where(x => x.TenantId == id &&
            x.RestockItem && x.LocalReturn.TenantId == id && x.LocalReturn.IsProcessed &&
            x.LocalReturn.ReturnDateUtc >= start && x.LocalReturn.ReturnDateUtc < end)
            .Select(x => new { x.UnitCostPrice, x.UnitQuantity, x.Quantity }).ToListAsync(cancellationToken);
        cost -= restocked.Sum(x => x.UnitCostPrice * (x.UnitQuantity > 0 ? x.UnitQuantity : x.Quantity));
        var damage = damages.Sum(x => Math.Abs(x.QuantityChange) * x.UnitCost);
        var profit = revenue - refunds - cost - damage;
        var payments = sales.SelectMany(x => x.Payments).ToList();
        return new DashboardSummaryResult
        {
            Revenue = revenue, Refunds = refunds, Cost = cost, Profit = profit,
            Margin = revenue == 0 ? 0 : profit / revenue * 100, SalesCount = sales.Count,
            AverageBasket = sales.Count == 0 ? 0 : revenue / sales.Count,
            CashRevenue = payments.Where(x => x.Method == PaymentMethod.Cash).Sum(x => x.Amount),
            CardRevenue = payments.Where(x => x.Method == PaymentMethod.Card).Sum(x => x.Amount),
            CreditRevenue = payments.Where(x => x.Method == PaymentMethod.Credit).Sum(x => x.Amount),
            TotalLoss = refunds + damage, LossRate = revenue == 0 ? 0 : (refunds + damage) / revenue * 100,
            RecentSales = sales.Take(10).Select(x => new RecentSaleResult
            {
                Id = x.Id, InvoiceNumber = x.LocalInvoiceNumber, SaleDate = x.SaleDateUtc.ToLocalTime(),
                CustomerName = x.CustomerLocalId.HasValue && customers.TryGetValue(x.CustomerLocalId.Value, out var name) ? name : "Walk-in Customer",
                TotalAmount = x.TotalAmount, PaymentMethod = string.Join(", ", x.Payments.Select(p => p.Method).Distinct()),
                PaymentSummary = string.Join(", ", x.Payments.GroupBy(p => p.Method).Select(g => $"{g.Key}: {g.Sum(p => p.Amount):N2}"))
            }).ToList(),
            TopProducts = sales.SelectMany(x => x.Lines).GroupBy(x => new { x.ProductLocalId, x.ProductName })
                .Select(g => new TopProductResult { ProductId = g.Key.ProductLocalId, ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(x => x.Quantity), TotalRevenue = g.Sum(x => x.LineTTC) })
                .OrderByDescending(x => x.TotalRevenue).Take(10).ToList()
        };
    }

    public async Task<ProfitAnalyticsResult> GetProfitAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var summary = await GetDashboardSummaryAsync(from, to, cancellationToken);
        return new ProfitAnalyticsResult { From = from, To = to, TotalRevenue = summary.Revenue,
            TotalCost = summary.Cost, TotalRefunds = summary.Refunds, TotalDamages = summary.TotalLoss - summary.Refunds,
            GrossProfit = summary.Profit, ProfitMargin = summary.Margin, CreditRevenue = summary.CreditRevenue };
    }

    public async Task<List<LossProductResult>> GetLossProductsAsync(DateOnly from, DateOnly to, int take = 10, CancellationToken cancellationToken = default)
    {
        var id = tenant.GetRequiredTenantId();
        var (start, end) = Bounds(from, to);
        var returns = await db.ReturnLines.AsNoTracking().Where(x => x.TenantId == id && x.LocalReturn.TenantId == id &&
            x.LocalReturn.IsProcessed && x.LocalReturn.ReturnDateUtc >= start && x.LocalReturn.ReturnDateUtc < end).ToListAsync(cancellationToken);
        var damages = await db.StockMovements.AsNoTracking().Where(x => x.TenantId == id && x.Type == StockMovementType.Damage &&
            x.MovementDateUtc >= start && x.MovementDateUtc < end).ToListAsync(cancellationToken);
        return returns.Select(x => new LossProductResult { ProductId = x.ProductLocalId, ProductName = x.ProductName,
                ReturnedQuantity = x.Quantity, LostRevenue = x.LineAmount, LossReason = "Return" })
            .Concat(damages.Select(x => new LossProductResult { ProductId = x.ProductLocalId, ProductName = x.ProductName,
                ReturnedQuantity = Math.Abs(x.QuantityChange), LostRevenue = Math.Abs(x.QuantityChange) * x.UnitCost, LossReason = "Damage" }))
            .GroupBy(x => new { x.ProductId, x.ProductName }).Select(g => new LossProductResult
            { ProductId = g.Key.ProductId, ProductName = g.Key.ProductName, ReturnedQuantity = g.Sum(x => x.ReturnedQuantity),
                LostRevenue = g.Sum(x => x.LostRevenue), LossReason = string.Join(", ", g.Select(x => x.LossReason).Distinct()) })
            .OrderByDescending(x => x.LostRevenue).Take(Math.Max(0, take)).ToList();
    }

    public async Task<WeeklyReportResult> GetWeeklyAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var id = tenant.GetRequiredTenantId();
        var (start, end) = Bounds(from, to);
        var summary = await GetDashboardSummaryAsync(from, to, cancellationToken);
        var count = await db.Returns.CountAsync(x => x.TenantId == id && x.IsProcessed && x.ReturnDateUtc >= start && x.ReturnDateUtc < end, cancellationToken);
        var sessions = await db.CashSessions.AsNoTracking().Where(x => x.TenantId == id &&
            ((x.OpenedAtUtc >= start && x.OpenedAtUtc < end) || (x.ClosedAtUtc >= start && x.ClosedAtUtc < end))).ToListAsync(cancellationToken);
        return new WeeklyReportResult { Week = $"{from:dd/MM} - {to:dd/MM}", Revenue = summary.Revenue,
            Expenses = summary.Cost + summary.TotalLoss, Profit = summary.Profit, SalesCount = summary.SalesCount, ReturnsCount = count,
            CashOpening = sessions.Where(x => x.OpenedAtUtc >= start && x.OpenedAtUtc < end).Sum(x => x.OpeningAmount),
            CashClosing = sessions.Where(x => x.ClosedAtUtc >= start && x.ClosedAtUtc < end).Sum(x => x.ClosingAmountCounted) };
    }

    public async Task<List<KeyValuePair<DateOnly, decimal>>> GetDailyRevenueAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var id = tenant.GetRequiredTenantId();
        var (start, end) = Bounds(from, to);
        var sales = await Sales(id, start, end).Select(x => new { x.SaleDateUtc, x.TotalAmount }).ToListAsync(cancellationToken);
        var totals = sales.GroupBy(x => DateOnly.FromDateTime(x.SaleDateUtc.ToLocalTime())).ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));
        return Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(offset => from.AddDays(offset))
            .Select(day => new KeyValuePair<DateOnly, decimal>(day, totals.GetValueOrDefault(day))).ToList();
    }
}
