using Inventory.Dto.Enums;
using Inventory.Dto.Returns.Requests;
using Inventory.LocalDB.Models;

namespace Inventory.Ui.Services.Sync;

public static class LocalReturnSyncMapper
{
    public static CreateCompleteReturnRequest
        ToCreateCompleteReturnRequest(
            LocalReturn localReturn)
    {
        ArgumentNullException.ThrowIfNull(
            localReturn);

        if (!localReturn.ServerSaleId.HasValue ||
            localReturn.ServerSaleId.Value ==
                Guid.Empty)
        {
            throw new InvalidOperationException(
                "The original sale must be synchronized before " +
                "the return can be uploaded.");
        }

        if (localReturn.Lines == null ||
            localReturn.Lines.Count ==
                0)
        {
            throw new InvalidOperationException(
                "The local return contains no lines.");
        }

        if (!Enum.TryParse<RefundMethod>(
                localReturn.RefundMethod,
                ignoreCase: true,
                out var refundMethod))
        {
            throw new InvalidOperationException(
                $"Unsupported refund method: " +
                $"{localReturn.RefundMethod}.");
        }

        if (refundMethod ==
            RefundMethod.Original)
        {
            throw new InvalidOperationException(
                "Original refund method is not supported.");
        }

        return new CreateCompleteReturnRequest
        {
            SaleId =
                localReturn.ServerSaleId.Value,

            ReturnDate =
                EnsureUtc(
                    localReturn.ReturnDateUtc),

            RefundType =
                refundMethod,

            Lines =
                localReturn.Lines
                    .Select(ToReturnLineItem)
                    .ToList()
        };
    }

    private static ReturnLineItem ToReturnLineItem(
        LocalReturnLine line)
    {
        if (!line.ProductServerId.HasValue ||
            line.ProductServerId.Value ==
                Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Product '{line.ProductName}' must be synchronized " +
                "before this return can be uploaded.");
        }

        if (line.Quantity <=
            0m)
        {
            throw new InvalidOperationException(
                $"Return quantity must be greater than zero for " +
                $"'{line.ProductName}'.");
        }

        if (line.UnitPrice <
            0m)
        {
            throw new InvalidOperationException(
                $"Return unit price cannot be negative for " +
                $"'{line.ProductName}'.");
        }

        if (line.VatRate <
                0m ||
            line.VatRate >
                100m)
        {
            throw new InvalidOperationException(
                $"Invalid VAT rate for '{line.ProductName}'.");
        }

        if (string.IsNullOrWhiteSpace(
                line.Reason))
        {
            throw new InvalidOperationException(
                $"A return reason is required for " +
                $"'{line.ProductName}'.");
        }

        /*
         * ProductId is the sold product:
         * - normal product for a unit sale;
         * - pack product for a pack sale.
         *
         * The server must resolve pack -> unit stock product.
         */
        return new ReturnLineItem
        {
            ProductId =
                line.ProductServerId.Value,

            Quantity =
                line.Quantity,

            /*
             * Effective VAT-included unit price after the original
             * sale discount.
             */
            UnitPrice =
                line.UnitPrice,

            VatRate =
                line.VatRate,

            Reason =
                line.Reason.Trim(),

            RestockItem =
                line.RestockItem
        };
    }

    private static DateTime EnsureUtc(
        DateTime value)
    {
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
}
