using Inventory.Dto.Analytics.Results;
using Inventory.Infrastructure.Data;
using Inventory.Services.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services.Features.Analytics.Profit
{
    public class GetProfitAnalyticsHandler
    : IRequestHandler<GetProfitAnalyticsQuery, ProfitAnalyticsResult>
    {
        private readonly InventoryDbContext _db;
        private readonly ITenantContext _tenant;

        public GetProfitAnalyticsHandler(InventoryDbContext db, ITenantContext tenant)
        {
            _db = db;
            _tenant = tenant;
        }

        public async Task<ProfitAnalyticsResult> Handle(
    GetProfitAnalyticsQuery request,
    CancellationToken ct)
        {
            var from = request.From ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
            var to = request.To ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var fromUtc = DateTime.SpecifyKind(
                from.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc
            );

            var toUtc = DateTime.SpecifyKind(
                to.ToDateTime(TimeOnly.MaxValue),
                DateTimeKind.Utc
            );

            var revenue = await _db.Sales
                .Where(s =>
                    s.TenantId == _tenant.TenantId &&
                    !s.IsDeleted &&
                    s.SaleDate >= fromUtc &&
                    s.SaleDate <= toUtc)
                .SumAsync(s => s.TotalAmount, ct);

            var refunds = await _db.Returns
                .Where(r =>
                    r.TenantId == _tenant.TenantId &&
                    !r.IsDeleted &&
                    r.ReturnDate >= fromUtc &&
                    r.ReturnDate <= toUtc)
                .SumAsync(r => r.TotalAmount, ct);

            var cost = await _db.PurchaseLines
                .Where(p =>
                    p.TenantId == _tenant.TenantId &&
                    p.Purchase.PurchaseDate >= fromUtc &&
                    p.Purchase.PurchaseDate <= toUtc)
                .SumAsync(p => p.QuantityReceived * p.UnitPurchasePrice, ct);

            var profit = revenue - refunds - cost;

            return new ProfitAnalyticsResult
            {
                From = from,
                To = to,
                TotalRevenue = revenue,
                TotalRefunds = refunds,
                TotalCost = cost,
                GrossProfit = profit,
                ProfitMargin = revenue == 0 ? 0 : profit / revenue * 100
            };
        }

    }

}
