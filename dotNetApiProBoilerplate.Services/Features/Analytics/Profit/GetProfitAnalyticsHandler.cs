using Inventory.Dto.Analytics.Results;
using Inventory.Dto.Enums;
using Inventory.Infrastructure.Data;
using Inventory.Services.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services.Features.Analytics.Profit;

public sealed class GetProfitAnalyticsHandler
    : IRequestHandler<
        GetProfitAnalyticsQuery,
        ProfitAnalyticsResult>
{
    private readonly InventoryDbContext _db;
    private readonly ITenantContext _tenant;

    public GetProfitAnalyticsHandler(
        InventoryDbContext db,
        ITenantContext tenant)
    {
        _db =
            db;

        _tenant =
            tenant;
    }

    public async Task<ProfitAnalyticsResult> Handle(
        GetProfitAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var businessTimeZone =
            GetBusinessTimeZone();

        var businessToday =
            DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    businessTimeZone));

        var from =
            request.From ??
            businessToday.AddDays(-30);

        var to =
            request.To ??
            businessToday;

        if (to < from)
        {
            throw new ArgumentException(
                "The analytics end date cannot be earlier " +
                "than the start date.");
        }

        var (
            fromUtc,
            toExclusiveUtc) =
            BuildUtcRange(
                from,
                to,
                businessTimeZone);

        var tenantId =
            _tenant.TenantId;

        /*
         * Total revenue:
         * includes Cash, Card, Credit and other payment methods.
         */
        var totalRevenue =
            await _db.Payments
                .AsNoTracking()
                .Where(payment =>
                    payment.TenantId == tenantId &&
                    !payment.IsDeleted &&
                    payment.PaidAt >= fromUtc &&
                    payment.PaidAt < toExclusiveUtc)
                .SumAsync(
                    payment =>
                        payment.Amount,
                    cancellationToken);

        /*
         * Credit is included in TotalRevenue but is also returned
         * separately for the Credit KPI.
         */
        var creditRevenue =
            await _db.Payments
                .AsNoTracking()
                .Where(payment =>
                    payment.TenantId == tenantId &&
                    !payment.IsDeleted &&
                    payment.PaidAt >= fromUtc &&
                    payment.PaidAt < toExclusiveUtc &&
                    payment.Method == PaymentMethod.Credit)
                .SumAsync(
                    payment =>
                        payment.Amount,
                    cancellationToken);

        /*
         * Refunds paid through non-credit methods.
         */
        var refunds =
            await _db.Returns
                .AsNoTracking()
                .Where(item =>
                    item.TenantId == tenantId &&
                    !item.IsDeleted &&
                    item.ReturnDate >= fromUtc &&
                    item.ReturnDate < toExclusiveUtc &&
                    item.RefundMethod != RefundMethod.Credit)
                .SumAsync(
                    item =>
                        item.TotalAmount,
                    cancellationToken);

        /*
         * Real inventory losses.
         */
        var damages =
            await _db.StockMovements
                .AsNoTracking()
                .Where(movement =>
                    movement.TenantId == tenantId &&
                    !movement.IsDeleted &&
                    movement.Type ==
                        StockMovementType.Damage &&
                    movement.MovementDate >= fromUtc &&
                    movement.MovementDate < toExclusiveUtc)
                .SumAsync(
                    movement =>
                        Math.Abs(
                            movement.QuantityChange) *
                        movement.UnitCost,
                    cancellationToken);

        /*
         * Cost of goods sold.
         *
         * The date filter is based on the sale business date,
         * not SaleLine.CreatedAt.
         */
        var totalCost =
            await _db.SaleLines
                .AsNoTracking()
                .Where(line =>
                    line.TenantId == tenantId &&
                    !line.IsDeleted &&
                    line.Sale != null &&
                    !line.Sale.IsDeleted &&
                    line.Sale.Status ==
                        SaleStatus.Completed &&
                    line.Sale.SaleDate >= fromUtc &&
                    line.Sale.SaleDate < toExclusiveUtc)
                .SumAsync(
                    line =>
                        line.UnitCostPrice *
                        (
                            line.UnitQuantity > 0m
                                ? line.UnitQuantity
                                : line.Quantity
                        ),
                    cancellationToken);

        var netRevenue =
            totalRevenue -
            refunds -
            damages;

        var grossProfit =
            netRevenue -
            totalCost;

        var profitMargin =
            netRevenue == 0m
                ? 0m
                : grossProfit /
                  netRevenue *
                  100m;

        return new ProfitAnalyticsResult
        {
            From =
                from,

            To =
                to,

            TotalRevenue =
                RoundMoney(
                    totalRevenue),

            CreditRevenue =
                RoundMoney(
                    creditRevenue),

            TotalRefunds =
                RoundMoney(
                    refunds),

            TotalDamages =
                RoundMoney(
                    damages),

            TotalCost =
                RoundMoney(
                    totalCost),

            GrossProfit =
                RoundMoney(
                    grossProfit),

            ProfitMargin =
                Math.Round(
                    profitMargin,
                    2,
                    MidpointRounding.AwayFromZero)
        };
    }

    private static (
        DateTime FromUtc,
        DateTime ToExclusiveUtc)
        BuildUtcRange(
            DateOnly from,
            DateOnly to,
            TimeZoneInfo businessTimeZone)
    {
        var localFrom =
            DateTime.SpecifyKind(
                from.ToDateTime(
                    TimeOnly.MinValue),
                DateTimeKind.Unspecified);

        /*
         * Borne supérieure exclusive :
         * le jour suivant à 00:00.
         */
        var localToExclusive =
            DateTime.SpecifyKind(
                to
                    .AddDays(1)
                    .ToDateTime(
                        TimeOnly.MinValue),
                DateTimeKind.Unspecified);

        var fromUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localFrom,
                businessTimeZone);

        var toExclusiveUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localToExclusive,
                businessTimeZone);

        return (
            fromUtc,
            toExclusiveUtc);
    }

    private static TimeZoneInfo GetBusinessTimeZone()
    {
        /*
         * Linux, Android, macOS and most containers.
         */
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Europe/Brussels");
        }
        catch (TimeZoneNotFoundException)
        {
            /*
             * Windows fallback.
             */
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Romance Standard Time");
        }
    }

    private static decimal RoundMoney(
        decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }
}