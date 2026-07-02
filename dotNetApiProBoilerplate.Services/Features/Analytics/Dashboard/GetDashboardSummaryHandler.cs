using Inventory.Dto.Analytics.Results;
using Inventory.Dto.Enums;
using Inventory.Infrastructure.Data;
using Inventory.Services.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Analytics.Dashboard
{
    public class GetDashboardSummaryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryResult>
    {
        private readonly InventoryDbContext _db;
        private readonly ITenantContext _tenant;

        public GetDashboardSummaryHandler(
            InventoryDbContext db,
            ITenantContext tenant)
        {
            _db = db;
            _tenant = tenant;
        }

        public async Task<DashboardSummaryResult> Handle(
            GetDashboardSummaryQuery request,
            CancellationToken ct)
        {
            var fromUtc = request.From.HasValue
                ? DateTime.SpecifyKind(request.From.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
                : DateTime.SpecifyKind(DateOnly.FromDateTime(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

            var toUtc = request.To.HasValue
                ? DateTime.SpecifyKind(request.To.Value.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc)
                : DateTime.SpecifyKind(DateOnly.FromDateTime(DateTime.UtcNow).ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

            var salesQuery = _db.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.Lines)
                    .ThenInclude(l => l.Product)
                .Include(s => s.Payments)
                .Where(s =>
                    s.TenantId == _tenant.TenantId &&
                    !s.IsDeleted &&
                    s.SaleDate >= fromUtc &&
                    s.SaleDate <= toUtc &&
                    s.Status != SaleStatus.Pending);

            var sales = await salesQuery.ToListAsync(ct);

            var revenue = sales.Sum(s => s.TotalAmount);

            var refunds = await _db.ReturnLines
                .AsNoTracking()
                .Where(r =>
                    r.TenantId == _tenant.TenantId &&
                    r.CreatedAt >= fromUtc &&
                    r.CreatedAt <= toUtc)
                .SumAsync(r => r.Quantity * r.UnitPrice, ct);


            var cost = sales
                .SelectMany(s => s.Lines)
                .Sum(l => l.Quantity * l.Product.PurchasePrice);

            var profit = revenue - refunds - cost;

            var salesCount = sales.Count;

            var cashRevenue = sales
                .SelectMany(s => s.Payments)
                .Where(p => p.Method == PaymentMethod.Cash)
                .Sum(p => p.Amount);

            var cardRevenue = sales
                .SelectMany(s => s.Payments)
                .Where(p => p.Method == PaymentMethod.Card)
                .Sum(p => p.Amount);

            var creditRevenue = sales
                .SelectMany(s => s.Payments)
                .Where(p => p.Method == PaymentMethod.Credit)
                .Sum(p => p.Amount);

            var returnLoss = await _db.ReturnLines
                .AsNoTracking()
                .Where(rl =>
                    rl.TenantId == _tenant.TenantId &&
                    !rl.RestockItem &&
                    rl.CreatedAt >= fromUtc &&
                    rl.CreatedAt <= toUtc)
                .SumAsync(rl => rl.Quantity * rl.UnitPrice, ct);

            var damageLoss = await _db.Damages
                .AsNoTracking()
                .Where(d =>
                    d.TenantId == _tenant.TenantId &&
                    !d.IsDeleted &&
                    d.DamageDate >= fromUtc &&
                    d.DamageDate <= toUtc)
                .SumAsync(d => d.EstimatedValue, ct);

            var totalLoss = returnLoss + damageLoss;

            var recentSales = sales
                .OrderByDescending(s => s.SaleDate)
                .Take(8)
                .Select(s => new RecentSaleResult
                {
                    Id = s.Id,
                    InvoiceNumber = s.InvoiceNumber,
                    SaleDate = s.SaleDate,
                    CustomerName = s.Customer != null ? s.Customer.Name : "Walk-in",
                    TotalAmount = s.TotalAmount,
                    PaymentSummary = string.Join(" / ", s.Payments.Select(p => p.Method).Distinct())
                })
                .ToList();

            var topProducts = sales
                .SelectMany(s => s.Lines)
                .GroupBy(l => new
                {
                    l.ProductId,
                    l.Product.Name
                })
                .Select(g => new TopProductResult
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(5)
                .ToList();

            return new DashboardSummaryResult
            {
                Revenue = revenue,
                Refunds = refunds,
                Cost = cost,
                Profit = profit,
                Margin = revenue > 0 ? profit / revenue * 100 : 0,

                SalesCount = salesCount,
                AverageBasket = salesCount > 0 ? revenue / salesCount : 0,

                CashRevenue = cashRevenue,
                CardRevenue = cardRevenue,
                CreditRevenue = creditRevenue,

                TotalLoss = totalLoss,
                LossRate = revenue > 0 ? totalLoss / revenue * 100 : 0,

                RecentSales = recentSales,
                TopProducts = topProducts
            };
        }
    }
}
