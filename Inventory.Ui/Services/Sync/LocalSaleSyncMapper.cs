using Inventory.Dto.Sales.Requests;
using Inventory.LocalDB.Models;

namespace Inventory.Ui.Services.Sync;

public static class LocalSaleSyncMapper
{
    public static CreateCompleteSaleRequest
        ToCreateCompleteSaleRequest(
            LocalSale sale)
    {
        ArgumentNullException.ThrowIfNull(sale);

        if (!sale.CashSessionServerId.HasValue ||
            sale.CashSessionServerId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Sale '{sale.LocalInvoiceNumber}' cannot be " +
                "synchronized because its cash session has not " +
                "been synchronized yet.");
        }

        if (sale.Lines == null ||
            sale.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                $"Sale '{sale.LocalInvoiceNumber}' has no lines.");
        }

        if (sale.Payments == null ||
            sale.Payments.Count == 0)
        {
            throw new InvalidOperationException(
                $"Sale '{sale.LocalInvoiceNumber}' has no payments.");
        }

        /*
         * A sale linked to a local customer must wait until that
         * customer has a server identifier.
         *
         * Otherwise the API would receive CustomerId = null and
         * incorrectly create a walk-in sale.
         */
        if (sale.CustomerLocalId.HasValue &&
            sale.CustomerLocalId.Value != Guid.Empty &&
            (!sale.CustomerServerId.HasValue ||
             sale.CustomerServerId.Value == Guid.Empty))
        {
            throw new InvalidOperationException(
                $"Sale '{sale.LocalInvoiceNumber}' cannot be " +
                "synchronized because its customer has not been " +
                "synchronized yet.");
        }

        var lines =
            sale.Lines
                .Select(ToSaleLineItem)
                .ToList();

        var payments =
            sale.Payments
                .Select(ToPaymentInfo)
                .ToList();

        return new CreateCompleteSaleRequest
        {
            CustomerId =
                sale.CustomerServerId,

            CashSessionId =
                sale.CashSessionServerId.Value,

            SaleDate =
                EnsureUtc(
                    sale.SaleDateUtc),

            Notes =
                BuildNotes(sale),

            /*
             * The server currently calculates its totals from
             * individual lines. This header value is retained for
             * compatibility, but must not be applied a second time.
             */
            DiscountAmount =
                0m,

            ChangeAmount =
                RoundMoney(
                    sale.ChangeAmount),

            Lines =
                lines,

            Payments =
                payments
        };
    }

    private static SaleLineItem ToSaleLineItem(
        LocalSaleLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (!line.ProductServerId.HasValue ||
            line.ProductServerId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Product '{line.ProductName}' has not been " +
                "synchronized with the server.");
        }

        if (line.Quantity <= 0m)
        {
            throw new InvalidOperationException(
                $"Product '{line.ProductName}' has an invalid quantity.");
        }

        if (line.UnitPrice < 0m)
        {
            throw new InvalidOperationException(
                $"Product '{line.ProductName}' has an invalid price.");
        }

        if (line.VatRate < 0m ||
            line.VatRate > 100m)
        {
            throw new InvalidOperationException(
                $"Product '{line.ProductName}' has an invalid VAT rate.");
        }

        var effectiveDiscountPercent =
            CalculateEffectiveDiscountPercent(
                line);

        return new SaleLineItem
        {
            /*
             * For a pack this must remain the server identifier
             * of the pack product.
             *
             * The server resolves the unit product and deducts
             * Quantity × UnitsPerPack from unit stock.
             */
            ProductId =
                line.ProductServerId.Value,

            /*
             * For a pack this remains the number of packs sold.
             */
            Quantity =
                RoundQuantity(
                    line.Quantity),

            /*
             * IMPORTANT:
             *
             * Local UnitPrice is VAT-included.
             * The server SaleService also interprets UnitPrice as
             * VAT-included before calculating HT and VAT.
             *
             * Do not convert this value to VAT-excluded.
             */
            UnitPrice =
                RoundMoney(
                    line.UnitPrice),

            VatRate =
                Math.Round(
                    line.VatRate,
                    2,
                    MidpointRounding.AwayFromZero),

            /*
             * The API supports only DiscountPercent.
             * Local percent and fixed-amount discounts are merged
             * into one equivalent percentage.
             */
            DiscountPercent =
                effectiveDiscountPercent
        };
    }

    private static PaymentInfo ToPaymentInfo(
        LocalPayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (payment.Amount <= 0m)
        {
            throw new InvalidOperationException(
                "Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(
                payment.Method))
        {
            throw new InvalidOperationException(
                "Payment method is required.");
        }

        return new PaymentInfo
        {
            Amount =
                RoundMoney(
                    payment.Amount),

            PaymentMethod =
                payment.Method.Trim(),

            Reference =
                NormalizeNullable(
                    payment.TransactionRef)
        };
    }

    private static decimal CalculateEffectiveDiscountPercent(
        LocalSaleLine line)
    {
        var grossAmount =
            line.Quantity *
            line.UnitPrice;

        if (grossAmount <= 0m)
        {
            return 0m;
        }

        var percentageDiscountAmount =
            grossAmount *
            ClampPercentage(
                line.DiscountPercent) /
            100m;

        var fixedDiscountAmount =
            Math.Max(
                0m,
                line.DiscountAmount);

        var totalDiscountAmount =
            percentageDiscountAmount +
            fixedDiscountAmount;

        /*
         * A discount cannot exceed the full gross line amount.
         */
        totalDiscountAmount =
            Math.Min(
                totalDiscountAmount,
                grossAmount);

        var effectivePercentage =
            totalDiscountAmount /
            grossAmount *
            100m;

        return Math.Round(
            effectivePercentage,
            4,
            MidpointRounding.AwayFromZero);
    }

    private static string BuildNotes(
        LocalSale sale)
    {
        var parts =
            new List<string>
            {
                "Offline synchronization",
                $"Local invoice: {sale.LocalInvoiceNumber}"
            };

        if (!string.IsNullOrWhiteSpace(
                sale.Notes))
        {
            parts.Add(
                sale.Notes.Trim());
        }

        var notes =
            string.Join(
                " | ",
                parts);

        return notes.Length <= 1000
            ? notes
            : notes[..1000];
    }

    private static decimal ClampPercentage(
        decimal percentage)
    {
        return Math.Clamp(
            percentage,
            0m,
            100m);
    }

    private static decimal RoundMoney(
        decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static decimal RoundQuantity(
        decimal value)
    {
        return Math.Round(
            value,
            3,
            MidpointRounding.AwayFromZero);
    }

    private static DateTime EnsureUtc(
        DateTime value)
    {
        if (value == default)
        {
            return DateTime.UtcNow;
        }

        return value.Kind switch
        {
            DateTimeKind.Utc =>
                value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            _ =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc)
        };
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}