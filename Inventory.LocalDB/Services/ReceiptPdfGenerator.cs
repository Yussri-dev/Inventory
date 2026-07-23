using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Inventory.LocalDB.Services
{
    public sealed class ReceiptPdfGenerator : IReceiptPdfGenerator
    {
        private const float ReceiptWidthMillimeters =
            80f;

        private const float ReceiptMarginMillimeters =
            4f;

        private static readonly CultureInfo ReceiptCulture =
            CultureInfo.GetCultureInfo(
                "fr-BE");

        private readonly ILogger<ReceiptPdfGenerator> _logger;

        public ReceiptPdfGenerator(
            ILogger<ReceiptPdfGenerator> logger)
        {
            _logger =
                logger;
        }

        public Task<byte[]> GenerateAsync(
            ReceiptPrintDocument document,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                document);

            if (document.Snapshot == null)
            {
                throw new InvalidOperationException(
                    "The receipt snapshot is required.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            /*
             * QuestPDF effectue un travail synchrone et CPU-bound.
             * Task.Run empêche de bloquer le thread UI MAUI.
             */
            return Task.Run(
                () => GeneratePdf(
                    document,
                    cancellationToken),
                cancellationToken);
        }

        private byte[] GeneratePdf(
            ReceiptPrintDocument printDocument,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot =
                printDocument.Snapshot;

            _logger.LogInformation(
                "Generating PDF receipt {InvoiceNumber}. " +
                "Duplicate={Duplicate}, CopyNumber={CopyNumber}.",
                snapshot.InvoiceNumber,
                printDocument.IsDuplicate,
                printDocument.CopyNumber);

            var pdfDocument =
                Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        /*
                         * Ticket thermique de largeur fixe et hauteur
                         * dynamique selon le contenu.
                         */
                        page.ContinuousSize(
                            ReceiptWidthMillimeters,
                            Unit.Millimetre);

                        page.Margin(
                            ReceiptMarginMillimeters,
                            Unit.Millimetre);

                        page.PageColor(
                            Colors.White);

                        page.DefaultTextStyle(style =>
                            style
                                .FontSize(8)
                                .FontColor(
                                    Colors.Black));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(
                                    4);

                                column.Item()
                                    .Element(container =>
                                        ComposeHeader(
                                            container,
                                            printDocument));

                                column.Item()
                                    .Element(
                                        ComposeDivider);

                                column.Item()
                                    .Element(container =>
                                        ComposeSaleInformation(
                                            container,
                                            snapshot));

                                column.Item()
                                    .Element(
                                        ComposeDivider);

                                column.Item()
                                    .Element(container =>
                                        ComposeLines(
                                            container,
                                            snapshot));

                                column.Item()
                                    .Element(
                                        ComposeDivider);

                                column.Item()
                                    .Element(container =>
                                        ComposeTotals(
                                            container,
                                            snapshot));

                                if (snapshot.VatSummary.Count > 0)
                                {
                                    column.Item()
                                        .Element(
                                            ComposeDivider);

                                    column.Item()
                                        .Element(container =>
                                            ComposeVatSummary(
                                                container,
                                                snapshot));
                                }

                                column.Item()
                                    .Element(
                                        ComposeDivider);

                                column.Item()
                                    .Element(container =>
                                        ComposePayments(
                                            container,
                                            snapshot));

                                column.Item()
                                    .Element(
                                        ComposeDivider);

                                column.Item()
                                    .Element(container =>
                                        ComposeFooter(
                                            container,
                                            snapshot));
                            });
                    });
                });

            var bytes =
                pdfDocument.GeneratePdf();

            cancellationToken.ThrowIfCancellationRequested();

            if (bytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "QuestPDF generated an empty receipt.");
            }

            _logger.LogInformation(
                "PDF receipt {InvoiceNumber} generated successfully. " +
                "Size={Size} bytes.",
                snapshot.InvoiceNumber,
                bytes.Length);

            return bytes;
        }

        private static void ComposeHeader(
            IContainer container,
            ReceiptPrintDocument printDocument)
        {
            var snapshot =
                printDocument.Snapshot;

            container
                .AlignCenter()
                .Column(column =>
                {
                    column.Spacing(
                        2);

                    column.Item()
                        .AlignCenter()
                        .Text(
                            snapshot.CompanyName)
                        .Bold()
                        .FontSize(
                            14);

                    AddCenteredText(
                        column,
                        snapshot.CompanyAddress);

                    if (!string.IsNullOrWhiteSpace(
                            snapshot.CompanyPhone))
                    {
                        AddCenteredText(
                            column,
                            $"Tél. {snapshot.CompanyPhone}");
                    }

                    AddCenteredText(
                        column,
                        snapshot.CompanyEmail);

                    if (!string.IsNullOrWhiteSpace(
                            snapshot.CompanyTaxNumber))
                    {
                        AddCenteredText(
                            column,
                            $"TVA : {snapshot.CompanyTaxNumber}");
                    }

                    column.Item()
                        .PaddingTop(
                            3)
                        .AlignCenter()
                        .Text(
                            "TICKET DE CAISSE")
                        .SemiBold()
                        .FontSize(
                            10);

                    if (printDocument.IsDuplicate)
                    {
                        column.Item()
                            .PaddingTop(
                                3)
                            .Border(
                                1)
                            .Padding(
                                3)
                            .AlignCenter()
                            .Text(
                                "DUPLICATA")
                            .Bold()
                            .FontSize(
                                13);

                        column.Item()
                            .AlignCenter()
                            .Text(
                                $"Copie n° {printDocument.CopyNumber}")
                            .SemiBold();

                        column.Item()
                            .AlignCenter()
                            .Text(
                                $"Réimprimé le " +
                                $"{ToLocal(printDocument.PrintedAtUtc):dd/MM/yyyy HH:mm}");

                        if (!string.IsNullOrWhiteSpace(
                                printDocument.Reason))
                        {
                            column.Item()
                                .AlignCenter()
                                .Text(
                                    $"Motif : {printDocument.Reason}")
                                .FontSize(
                                    7);
                        }
                    }
                    else
                    {
                        column.Item()
                            .AlignCenter()
                            .Text(
                                "ORIGINAL")
                            .SemiBold();
                    }
                });
        }

        private static void ComposeSaleInformation(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            container.Column(column =>
            {
                column.Spacing(
                    2);

                column.Item()
                    .Element(item =>
                        ComposeTwoColumns(
                            item,
                            "Ticket",
                            snapshot.InvoiceNumber));

                column.Item()
                    .Element(item =>
                        ComposeTwoColumns(
                            item,
                            "Date",
                            ToLocal(
                                    snapshot.SaleDateUtc)
                                .ToString(
                                    "dd/MM/yyyy HH:mm",
                                    ReceiptCulture)));

                if (!string.IsNullOrWhiteSpace(
                        snapshot.CashierName))
                {
                    column.Item()
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                "Caissier",
                                snapshot.CashierName));
                }

                column.Item()
                    .Element(item =>
                        ComposeTwoColumns(
                            item,
                            "Client",
                            string.IsNullOrWhiteSpace(
                                snapshot.CustomerName)
                                    ? "Client comptoir"
                                    : snapshot.CustomerName));
            });
        }

        private static void ComposeLines(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            container.Column(column =>
            {
                column.Spacing(
                    5);

                foreach (var line in snapshot.Lines)
                {
                    column.Item()
                        .Column(lineColumn =>
                        {
                            lineColumn.Spacing(
                                1);

                            lineColumn.Item()
                                .Text(
                                    line.ProductName)
                                .SemiBold()
                                .FontSize(
                                    8.5f);

                            if (!string.IsNullOrWhiteSpace(
                                    line.Barcode))
                            {
                                lineColumn.Item()
                                    .Text(
                                        $"Code : {line.Barcode}")
                                    .FontSize(
                                        6.5f)
                                    .FontColor(
                                        Colors.Grey.Darken1);
                            }

                            lineColumn.Item()
                                .Element(item =>
                                    ComposeTwoColumns(
                                        item,
                                        $"{FormatQuantity(line.Quantity)} × " +
                                        $"{FormatMoney(line.UnitPrice)}",
                                        FormatMoney(
                                            line.TotalInclVat),
                                        rightBold: true));

                            if (line.DiscountPercent > 0m)
                            {
                                lineColumn.Item()
                                    .Element(item =>
                                        ComposeTwoColumns(
                                            item,
                                            $"Réduction " +
                                            $"{FormatPercentage(line.DiscountPercent)}",
                                            $"-{FormatMoney(line.TotalDiscount)}"));
                            }
                            else if (line.DiscountAmount > 0m)
                            {
                                lineColumn.Item()
                                    .Element(item =>
                                        ComposeTwoColumns(
                                            item,
                                            "Réduction",
                                            $"-{FormatMoney(line.TotalDiscount)}"));
                            }

                            lineColumn.Item()
                                .Element(item =>
                                    ComposeTwoColumns(
                                        item,
                                        $"TVA {FormatPercentage(line.VatRate)}",
                                        FormatMoney(
                                            line.VatAmount)));
                        });
                }
            });
        }

        private static void ComposeTotals(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            container.Column(column =>
            {
                column.Spacing(
                    2);

                column.Item()
                    .Element(item =>
                        ComposeTwoColumns(
                            item,
                            "Total HT",
                            FormatMoney(
                                snapshot.SubtotalExclVat)));

                column.Item()
                    .Element(item =>
                        ComposeTwoColumns(
                            item,
                            "TVA",
                            FormatMoney(
                                snapshot.TotalVat)));

                column.Item()
                    .PaddingTop(
                        3)
                    .BorderTop(
                        1)
                    .PaddingTop(
                        3)
                    .Element(item =>
                        ComposeTwoColumns(
                            item,
                            "TOTAL",
                            FormatMoney(
                                snapshot.TotalAmount),
                            leftBold: true,
                            rightBold: true,
                            fontSize: 12));
            });
        }

        private static void ComposeVatSummary(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            container.Column(column =>
            {
                column.Spacing(
                    2);

                column.Item()
                    .Text(
                        "DÉTAIL TVA")
                    .SemiBold()
                    .FontSize(
                        8.5f);

                foreach (var vat in snapshot.VatSummary)
                {
                    column.Item()
                        .PaddingTop(
                            2)
                        .Text(
                            $"TVA {FormatPercentage(vat.VatRate)}")
                        .SemiBold();

                    column.Item()
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                "Base HT",
                                FormatMoney(
                                    vat.AmountExclVat)));

                    column.Item()
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                "Montant TVA",
                                FormatMoney(
                                    vat.VatAmount)));

                    column.Item()
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                "Total TTC",
                                FormatMoney(
                                    vat.AmountInclVat)));
                }
            });
        }

        private static void ComposePayments(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            container.Column(column =>
            {
                column.Spacing(
                    2);

                column.Item()
                    .Text(
                        "PAIEMENTS")
                    .SemiBold()
                    .FontSize(
                        8.5f);

                if (snapshot.Payments.Count == 0)
                {
                    column.Item()
                        .Text(
                            "Aucun paiement enregistré.")
                        .Italic()
                        .FontColor(
                            Colors.Grey.Darken1);

                    return;
                }

                foreach (var payment in snapshot.Payments)
                {
                    column.Item()
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                TranslatePaymentMethod(
                                    payment.Method),
                                FormatMoney(
                                    payment.Amount)));
                }

                if (snapshot.ChangeAmount > 0m)
                {
                    column.Item()
                        .PaddingTop(
                            3)
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                "MONNAIE",
                                FormatMoney(
                                    snapshot.ChangeAmount),
                                leftBold: true,
                                rightBold: true));
                }
            });
        }

        private static void ComposeFooter(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            container
                .PaddingTop(
                    4)
                .AlignCenter()
                .Column(column =>
                {
                    column.Spacing(
                        2);

                    if (!string.IsNullOrWhiteSpace(
                            snapshot.FooterText))
                    {
                        column.Item()
                            .AlignCenter()
                            .Text(
                                snapshot.FooterText)
                            .FontSize(
                                7.5f);
                    }

                    column.Item()
                        .AlignCenter()
                        .Text(
                            "Merci pour votre achat.")
                        .SemiBold();

                    column.Item()
                        .PaddingTop(
                            4)
                        .AlignCenter()
                        .Text(
                            snapshot.InvoiceNumber)
                        .FontSize(
                            6.5f)
                        .FontColor(
                            Colors.Grey.Darken1);
                });
        }

        private static void ComposeTwoColumns(
            IContainer container,
            string left,
            string right,
            bool leftBold = false,
            bool rightBold = false,
            float fontSize = 8f)
        {
            container.Row(row =>
            {
                var leftText =
                    row.RelativeItem()
                        .Text(
                            left)
                        .FontSize(
                            fontSize);

                var rightText =
                    row.ConstantItem(
                            78)
                        .AlignRight()
                        .Text(
                            right)
                        .FontSize(
                            fontSize);

                if (leftBold)
                {
                    leftText.Bold();
                }

                if (rightBold)
                {
                    rightText.Bold();
                }
            });
        }

        private static void ComposeDivider(
            IContainer container)
        {
            container
                .PaddingVertical(
                    2)
                .LineHorizontal(
                    0.5f)
                .LineColor(
                    Colors.Grey.Medium);
        }

        private static void AddCenteredText(
            ColumnDescriptor column,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return;
            }

            column.Item()
                .AlignCenter()
                .Text(
                    value)
                .FontSize(
                    7);
        }

        private static string TranslatePaymentMethod(
            string? method)
        {
            if (string.IsNullOrWhiteSpace(
                    method))
            {
                return "Paiement";
            }

            return method.Trim().ToLowerInvariant() switch
            {
                "cash" =>
                    "Espèces",

                "card" =>
                    "Carte",

                "credit" =>
                    "Crédit",

                "banktransfer" =>
                    "Virement",

                "bank transfer" =>
                    "Virement",

                _ =>
                    method.Trim()
            };
        }

        private static DateTime ToLocal(
            DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc =>
                    value.ToLocalTime(),

                DateTimeKind.Local =>
                    value,

                _ =>
                    DateTime.SpecifyKind(
                            value,
                            DateTimeKind.Utc)
                        .ToLocalTime()
            };
        }

        private static string FormatMoney(
            decimal amount)
        {
            return amount.ToString(
                       "N2",
                       ReceiptCulture) +
                   " €";
        }

        private static string FormatQuantity(
            decimal quantity)
        {
            return quantity.ToString(
                "0.###",
                ReceiptCulture);
        }

        private static string FormatPercentage(
            decimal percentage)
        {
            return percentage.ToString(
                       "0.##",
                       ReceiptCulture) +
                   " %";
        }
    }
}
