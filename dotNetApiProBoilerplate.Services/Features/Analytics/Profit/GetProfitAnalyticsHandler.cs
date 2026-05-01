using Inventory.Dto.Analytics.Results;
using Inventory.Dto.Enums;
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

            var fromUtc = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var toUtc = DateTime.SpecifyKind(to.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

            // PAYMENTS → source de vérité
            var payments = await _db.Payments
                .Where(p =>
                    p.TenantId == _tenant.TenantId &&
                    !p.IsDeleted &&
                    p.PaidAt >= fromUtc &&
                    p.PaidAt <= toUtc)
                .ToListAsync(ct);

            var cashRevenue = await _db.Payments
                .Where(p =>
                    p.TenantId == _tenant.TenantId &&
                    !p.IsDeleted &&
                    p.PaidAt >= fromUtc &&
                    p.PaidAt <= toUtc &&
                    p.Method != PaymentMethod.Credit)
                .SumAsync(p => p.Amount, ct);

            var creditRevenue = await _db.Payments
                .Where(p =>
                    p.TenantId == _tenant.TenantId &&
                    !p.IsDeleted &&
                    p.PaidAt >= fromUtc &&
                    p.PaidAt <= toUtc &&
                    p.Method == PaymentMethod.Credit)
                .SumAsync(p => p.Amount, ct);

            // REFUNDS (cash uniquement)
            var refunds = await _db.Returns
                .Where(r =>
                    r.TenantId == _tenant.TenantId &&
                    !r.IsDeleted &&
                    r.ReturnDate >= fromUtc &&
                    r.ReturnDate <= toUtc &&
                    r.RefundMethod != RefundMethod.Credit)
                .SumAsync(r => r.TotalAmount, ct);

            // DAMAGES (perte stock réelle)
            var damages = await _db.StockMovements
                .Where(sm =>
                    sm.TenantId == _tenant.TenantId &&
                    !sm.IsDeleted &&
                    sm.Type == StockMovementType.Damage &&
                    sm.MovementDate >= fromUtc &&
                    sm.MovementDate <= toUtc)
                .SumAsync(sm => Math.Abs(sm.QuantityChange) * sm.UnitCost, ct);

            // COST (COGS)
            var cost = await _db.SaleLines
                .Where(sl =>
                    sl.TenantId == _tenant.TenantId &&
                    !sl.IsDeleted &&
                    sl.Sale != null &&
                    !sl.Sale.IsDeleted &&
                    sl.Sale.SaleDate >= fromUtc &&
                    sl.Sale.SaleDate <= toUtc &&
                    sl.Sale.Status == SaleStatus.Completed)
                .SumAsync(sl =>
                    sl.UnitCostPrice *
                    (sl.UnitQuantity > 0 ? sl.UnitQuantity : sl.Quantity), ct);

            // CALCULS
            var netRevenue = cashRevenue - refunds - damages;
            var profit = netRevenue - cost;

            return new ProfitAnalyticsResult
            {
                From = from,
                To = to,

                TotalRevenue = cashRevenue,
                CreditRevenue = creditRevenue,

                TotalRefunds = refunds,
                TotalDamages = damages,
                TotalCost = cost,

                GrossProfit = profit,
                ProfitMargin = netRevenue == 0
                    ? 0
                    : profit / netRevenue * 100
            };
        }

    }

}
