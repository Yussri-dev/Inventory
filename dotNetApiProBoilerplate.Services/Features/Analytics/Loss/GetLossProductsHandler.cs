using Inventory.Dto.Analytics.Results;
using Inventory.Infrastructure.Data;
using Inventory.Services.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services.Features.Analytics.Loss
{
    public class GetLossProductsHandler
        : IRequestHandler<GetLossProductsQuery, LossProductsResponse>
    {
        private readonly InventoryDbContext _db;
        private readonly ITenantContext _tenant;

        public GetLossProductsHandler(
            InventoryDbContext db,
            ITenantContext tenant)
        {
            _db = db;
            _tenant = tenant;
        }

        public async Task<LossProductsResponse> Handle(
            GetLossProductsQuery request,
            CancellationToken ct)
        {
            // =========================
            // DATE RANGE (UTC SAFE)
            // =========================
            DateTime? fromUtc = request.From.HasValue
                ? DateTime.SpecifyKind(
                    request.From.Value.ToDateTime(TimeOnly.MinValue),
                    DateTimeKind.Utc)
                : null;

            DateTime? toUtc = request.To.HasValue
                ? DateTime.SpecifyKind(
                    request.To.Value.ToDateTime(TimeOnly.MaxValue),
                    DateTimeKind.Utc)
                : null;

            // =========================
            // RETURNS — NON RESTOCKED
            // =========================
            var returnLossQuery =
                from rl in _db.ReturnLines.AsNoTracking()
                where rl.TenantId == _tenant.TenantId
                      && !rl.RestockItem
                      && (!fromUtc.HasValue || rl.CreatedAt >= fromUtc)
                      && (!toUtc.HasValue || rl.CreatedAt <= toUtc)
                select new
                {
                    rl.ProductId,
                    ProductName = rl.Product.Name,
                    Quantity = rl.Quantity,
                    LostValue = rl.Quantity * rl.UnitPrice,
                    Reason = "Return"
                };

            // =========================
            // DAMAGES / CASSE
            // =========================
            var damageLossQuery =
                from d in _db.Damages.AsNoTracking()
                where d.TenantId == _tenant.TenantId
                      && !d.IsDeleted
                      && (!fromUtc.HasValue || d.DamageDate >= fromUtc)
                      && (!toUtc.HasValue || d.DamageDate <= toUtc)
                join p in _db.Products.AsNoTracking()
                    on d.ProductId equals p.Id
                select new
                {
                    d.ProductId,
                    ProductName = p.Name,
                    Quantity = d.Quantity,
                    LostValue = d.EstimatedValue,
                    Reason = "Damage"
                };

            // =========================
            // UNION + AGGREGATION
            // =========================
            var items = await returnLossQuery
                .Concat(damageLossQuery)
                .GroupBy(x => new { x.ProductId, x.ProductName })
                .Select(g => new LossProductResult
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ReturnedQuantity = g.Sum(x => x.Quantity),
                    LostRevenue = g.Sum(x => x.LostValue),
                    LossReason = string.Join(", ",
                        g.Select(x => x.Reason).Distinct())
                })
                .OrderByDescending(x => x.LostRevenue)
                .Take(request.Limit)
                .ToListAsync(ct);

            return new LossProductsResponse
            {
                Items = items,
                TotalLoss = items.Sum(i => i.LostRevenue)
            };
        }
    }
}
