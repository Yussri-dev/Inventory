using Inventory.Dto.Analytics.Results;
using Inventory.Infrastructure.Data;
using Inventory.Services.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Inventory.Services.Features.Analytics.WeeklyReport
{
    public class GetWeeklyReportHandler
    : IRequestHandler<GetWeeklyReportQuery, WeeklyReportResult>
    {
        private readonly InventoryDbContext _db;
        private readonly ITenantContext _tenant;

        public GetWeeklyReportHandler(InventoryDbContext db, ITenantContext tenant)
        {
            _db = db;
            _tenant = tenant;
        }

        public async Task<WeeklyReportResult> Handle(
     GetWeeklyReportQuery request,
     CancellationToken ct)
        {
            var date = ISOWeek.ToDateTime(
                request.Year ?? DateTime.UtcNow.Year,
                request.Week ?? ISOWeek.GetWeekOfYear(DateTime.UtcNow),
                DayOfWeek.Monday
            );

            var start = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            var end = start.AddDays(7);

            var revenue = await _db.Sales
                .Where(s =>
                    s.TenantId == _tenant.TenantId &&
                    s.SaleDate >= start &&
                    s.SaleDate < end)
                .SumAsync(s => s.TotalAmount, ct);

            var expenses = await _db.Purchases
                .Where(p =>
                    p.TenantId == _tenant.TenantId &&
                    p.PurchaseDate >= start &&
                    p.PurchaseDate < end)
                .SumAsync(p => p.TotalAmountInclVat, ct);

            var salesCount = await _db.Sales.CountAsync(s =>
                s.TenantId == _tenant.TenantId &&
                s.SaleDate >= start &&
                s.SaleDate < end, ct);

            var returnsCount = await _db.Returns.CountAsync(r =>
                r.TenantId == _tenant.TenantId &&
                r.ReturnDate >= start &&
                r.ReturnDate < end, ct);

            return new WeeklyReportResult
            {
                Week = $"{request.Year}-W{request.Week}",
                Revenue = revenue,
                Expenses = expenses,
                Profit = revenue - expenses,
                SalesCount = salesCount,
                ReturnsCount = returnsCount
            };
        }

    }


}
