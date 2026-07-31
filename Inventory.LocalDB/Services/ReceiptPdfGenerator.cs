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
        private const float ReceiptWidthMillimeters = 80f;
        private const float ReceiptMarginMillimeters = 3.5f;

        private static readonly CultureInfo ReceiptCulture =
            CultureInfo.GetCultureInfo("fr-BE");

        private readonly ILogger<ReceiptPdfGenerator> _logger;
        private readonly IReceiptBarcodeGenerator _barcodeGenerator;

        public ReceiptPdfGenerator(
            ILogger<ReceiptPdfGenerator> logger,
            IReceiptBarcodeGenerator barcodeGenerator)
        {
            _logger = logger;
            _barcodeGenerator = barcodeGenerator;
        }

        public Task<byte[]> GenerateAsync(
            ReceiptPrintDocument document,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(document.Snapshot);

            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = document.Snapshot;

            byte[]? barcodeImage = null;

            if (!string.IsNullOrWhiteSpace(snapshot.BarcodeValue))
            {
                barcodeImage =
                    _barcodeGenerator.GenerateCode128Png(
                        snapshot.BarcodeValue,
                        width: 460,
                        height: 110);
            }

            /*
             * QuestPDF effectue un travail synchrone et CPU-bound.
             * L'image du code-barres reste locale à cet appel afin que
             * le service soit sûr en cas de générations simultanées.
             */
            return Task.Run(
                () => GeneratePdf(
                    document,
                    barcodeImage,
                    cancellationToken),
                cancellationToken);
        }

        private byte[] GeneratePdf(
            ReceiptPrintDocument printDocument,
            byte[]? barcodeImage,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = printDocument.Snapshot;

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
                        page.ContinuousSize(
                            ReceiptWidthMillimeters,
                            Unit.Millimetre);

                        page.Margin(
                            ReceiptMarginMillimeters,
                            Unit.Millimetre);

                        page.PageColor(Colors.White);

                        page.DefaultTextStyle(style =>
                            style
                                .FontSize(8)
                                .FontColor(Colors.Black));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(3);

                                column.Item()
                                    .Element(container =>
                                        ComposeHeader(
                                            container,
                                            printDocument));

                                column.Item()
                                    .Element(ComposeDivider);

                                column.Item()
                                    .Element(container =>
                                        ComposeSaleInformation(
                                            container,
                                            snapshot));

                                column.Item()
                                    .Element(ComposeDivider);

                                column.Item()
                                    .Element(container =>
                                        ComposeLines(
                                            container,
                                            snapshot));

                                column.Item()
                                    .Element(ComposeDivider);

                                column.Item()
                                    .Element(container =>
                                        ComposeTotals(
                                            container,
                                            snapshot));

                                if (snapshot.VatSummary != null &&
                                    snapshot.VatSummary.Count > 0)
                                {
                                    column.Item()
                                        .Element(ComposeDivider);

                                    column.Item()
                                        .Element(container =>
                                            ComposeVatSummary(
                                                container,
                                                snapshot));
                                }

                                if (snapshot.Payments != null &&
                                    snapshot.Payments.Count > 0)
                                {
                                    column.Item()
                                        .Element(ComposeDivider);

                                    column.Item()
                                        .Element(container =>
                                            ComposePayments(
                                                container,
                                                snapshot));
                                }

                                if (barcodeImage is { Length: > 0 } &&
                                    !string.IsNullOrWhiteSpace(
                                        snapshot.BarcodeValue))
                                {
                                    column.Item()
                                        .Element(ComposeDivider);

                                    column.Item()
                                        .Element(container =>
                                            ComposeBarcodeSection(
                                                container,
                                                snapshot.BarcodeValue,
                                                barcodeImage));
                                }

                                column.Item()
                                    .Element(container =>
                                        ComposeFooter(
                                            container,
                                            snapshot));
                            });
                    });
                });

            var bytes = pdfDocument.GeneratePdf();

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
            var snapshot = printDocument.Snapshot;

            container
                .AlignCenter()
                .Column(column =>
                {
                    column.Spacing(1.5f);

                    if (snapshot.LogoBytes is { Length: > 0 })
                    {
                        column.Item()
                            .AlignCenter()
                            .Width(105)
                            .Image(snapshot.LogoBytes)
                            .FitArea();

                        column.Item()
                            .Height(2);
                    }

                    column.Item()
                        .AlignCenter()
                        .Text(snapshot.CompanyName)
                        .Bold()
                        .FontSize(
                            snapshot.LogoBytes is { Length: > 0 }
                                ? 10
                                : 14);

                    if (!string.IsNullOrWhiteSpace(
                            snapshot.HeaderTagLine))
                    {
                        column.Item()
                            .PaddingTop(1)
                            .AlignCenter()
                            .Text(
                                $"-{snapshot.HeaderTagLine.Trim()}-")
                            .FontSize(7.5f)
                            .LetterSpacing(0.5f);
                    }

                    AddCenteredText(
                        column,
                        snapshot.ReceiptHeader,
                        7.5f);

                    AddCenteredText(
                        column,
                        snapshot.CompanyAddress,
                        7.2f);

                    AddCenteredText(
                        column,
                        snapshot.ExtraAddressLine,
                        7.2f);

                    AddCenteredText(
                        column,
                        snapshot.CompanyEmail,
                        7.2f);

                    if (!string.IsNullOrWhiteSpace(
                            snapshot.CompanyPhone))
                    {
                        AddCenteredText(
                            column,
                            $"NUMBER : {snapshot.CompanyPhone}",
                            7.2f);
                    }

                    AddCenteredText(
                        column,
                        snapshot.SocialLine,
                        7.2f);

                    if (!string.IsNullOrWhiteSpace(
                            snapshot.CompanyTaxNumber))
                    {
                        AddCenteredText(
                            column,
                            $"TVA : {snapshot.CompanyTaxNumber}",
                            7f);
                    }

                    column.Item()
                        .PaddingTop(4)
                        .AlignCenter()
                        .Text("TICKET DE CAISSE")
                        .Bold()
                        .FontSize(11);

                    if (printDocument.IsDuplicate)
                    {
                        column.Item()
                            .PaddingTop(1)
                            .AlignCenter()
                            .Text("DUPLICATA")
                            .Bold()
                            .FontSize(10);

                        column.Item()
                            .AlignCenter()
                            .Text(
                                $"COPIE N° {printDocument.CopyNumber}")
                            .FontSize(7.5f);

                        column.Item()
                            .AlignCenter()
                            .Text(
                                $"RÉIMPRIMÉ LE " +
                                $"{ToLocal(printDocument.PrintedAtUtc):dd/MM/yyyy HH:mm}")
                            .FontSize(6.8f);

                        if (!string.IsNullOrWhiteSpace(
                                printDocument.Reason))
                        {
                            column.Item()
                                .AlignCenter()
                                .Text(
                                    $"MOTIF : {printDocument.Reason}")
                                .FontSize(6.5f);
                        }
                    }
                    else
                    {
                        column.Item()
                            .AlignCenter()
                            .Text("ORIGINAL")
                            .SemiBold()
                            .FontSize(9);
                    }
                });
        }

        private static void ComposeSaleInformation(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            container.Column(column =>
            {
                column.Spacing(2);

                column.Item()
                    .AlignCenter()
                    .Text(
                        $"N° TICKET : {snapshot.InvoiceNumber}")
                    .FontSize(7.8f);

                column.Item()
                    .Element(item =>
                        ComposeTwoColumns(
                            item,
                            "CUSTOMER",
                            string.IsNullOrWhiteSpace(
                                snapshot.CustomerName)
                                    ? "CLIENT PASSAGER"
                                    : snapshot.CustomerName
                                        .Trim()
                                        .ToUpperInvariant()));

                if (!string.IsNullOrWhiteSpace(
                        snapshot.CashierName))
                {
                    column.Item()
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                "CAISSIER",
                                snapshot.CashierName
                                    .Trim()
                                    .ToUpperInvariant()));
                }
            });
        }

        private static void ComposeLines(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            container.Column(column =>
            {
                column.Spacing(4);

                column.Item()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(5);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.8f);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell()
                                .Text("DESIGNATION")
                                .Bold()
                                .FontSize(7.4f);

                            header.Cell()
                                .AlignRight()
                                .Text("QTE")
                                .Bold()
                                .FontSize(7.4f);

                            header.Cell()
                                .AlignRight()
                                .Text("PRICE")
                                .Bold()
                                .FontSize(7.4f);

                            header.Cell()
                                .AlignRight()
                                .Text("AMOUNT")
                                .Bold()
                                .FontSize(7.4f);
                        });

                        foreach (var line in snapshot.Lines)
                        {
                            table.Cell()
                                .Column(productColumn =>
                                {
                                    productColumn.Spacing(0.5f);

                                    productColumn.Item()
                                        .Text(line.ProductName)
                                        .FontSize(7.7f);

                                    //if (!string.IsNullOrWhiteSpace(
                                    //        line.Barcode))
                                    //{
                                    //    productColumn.Item()
                                    //        .Text(
                                    //            $"Code : {line.Barcode}")
                                    //        .FontSize(6.1f)
                                    //        .FontColor(
                                    //            Colors.Grey.Darken1);
                                    //}

                                    if (line.DiscountPercent > 0m)
                                    {
                                        productColumn.Item()
                                            .Text(
                                                $"REDUCTION " +
                                                $"{FormatPercentage(line.DiscountPercent)}")
                                            .FontSize(6.2f);
                                    }
                                    else if (line.DiscountAmount > 0m)
                                    {
                                        productColumn.Item()
                                            .Text("REDUCTION")
                                            .FontSize(6.2f);
                                    }
                                });

                            table.Cell()
                                .AlignRight()
                                .Text(
                                    FormatQuantity(
                                        line.Quantity))
                                .FontSize(7.5f);

                            table.Cell()
                                .AlignRight()
                                .Text(
                                    FormatAmountNoSymbol(
                                        line.UnitPrice))
                                .FontSize(7.5f);

                            table.Cell()
                                .AlignRight()
                                .Text(
                                    FormatAmountNoSymbol(
                                        line.TotalInclVat))
                                .SemiBold()
                                .FontSize(7.5f);
                        }
                    });

                column.Item()
                    .PaddingTop(2)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Text(
                                $"ROW NUMBER : " +
                                $"{snapshot.Lines.Count}")
                            .FontSize(7.3f);

                        row.RelativeItem()
                            .AlignRight()
                            .Text(
                                $"TOTAL QTY : " +
                                $"{FormatQuantity(snapshot.Lines.Sum(line => line.Quantity))}")
                            .FontSize(7.3f);
                    });
            });
        }

        private static void ComposeTotals(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            var currency =
                ResolveCurrencyCode(snapshot);

            var totalReceived =
                snapshot.TotalReceived > 0m
                    ? snapshot.TotalReceived
                    : snapshot.Payments?
                        .Sum(payment => payment.Amount) ??
                      0m;

            container.Column(column =>
            {
                column.Spacing(2);

                column.Item()
                    .PaddingVertical(3)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .AlignRight()
                            .Text("TOTAL :")
                            .Bold()
                            .FontSize(12);

                        row.ConstantItem(118)
                            .AlignRight()
                            .Text(
                                $"{FormatAmountNoSymbol(snapshot.TotalAmount)} " +
                                $"{currency}")
                            .Bold()
                            .FontSize(12);
                    });

                //column.Item()
                //    .Element(item =>
                //        ComposeTwoColumns(
                //            item,
                //            "TOTAL HT",
                //            FormatMoney(
                //                snapshot.SubtotalExclVat,
                //                currency)));

                //column.Item()
                //    .Element(item =>
                //        ComposeTwoColumns(
                //            item,
                //            "TVA",
                //            FormatMoney(
                //                snapshot.TotalVat,
                //                currency)));

                column.Item()
                    .PaddingTop(3)
                    .Element(item =>
                        ComposeTwoColumns(
                            item,
                            "REÇU",
                            FormatMoney(
                                totalReceived,
                                currency)));

                column.Item()
                    .Element(item =>
                        ComposeTwoColumns(
                            item,
                            "MONNAIE",
                            FormatMoney(
                                snapshot.ChangeAmount,
                                currency),
                            leftBold: snapshot.ChangeAmount > 0m,
                            rightBold: snapshot.ChangeAmount > 0m));
            });
        }

        private static void ComposeVatSummary(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            var currency =
                ResolveCurrencyCode(snapshot);

            container.Column(column =>
            {
                column.Spacing(2);

                column.Item()
                    .Text("DÉTAIL TVA")
                    .SemiBold()
                    .FontSize(8.5f);

                foreach (var vat in snapshot.VatSummary)
                {
                    column.Item()
                        .PaddingTop(2)
                        .Text(
                            $"TVA {FormatPercentage(vat.VatRate)}")
                        .SemiBold()
                        .FontSize(7.6f);

                    column.Item()
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                "Base HT",
                                FormatMoney(
                                    vat.AmountExclVat,
                                    currency)));

                    column.Item()
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                "Montant TVA",
                                FormatMoney(
                                    vat.VatAmount,
                                    currency)));

                    column.Item()
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                "Total TTC",
                                FormatMoney(
                                    vat.AmountInclVat,
                                    currency)));
                }
            });
        }

        private static void ComposePayments(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            var currency =
                ResolveCurrencyCode(snapshot);

            container.Column(column =>
            {
                column.Spacing(2);

                column.Item()
                    .Text("PAIEMENTS")
                    .SemiBold()
                    .FontSize(8.5f);

                foreach (var payment in snapshot.Payments)
                {
                    column.Item()
                        .Element(item =>
                            ComposeTwoColumns(
                                item,
                                TranslatePaymentMethod(
                                    payment.Method),
                                FormatMoney(
                                    payment.Amount,
                                    currency)));
                }
            });
        }

        private static void ComposeBarcodeSection(
     IContainer container,
     string barcodeValue,
     byte[] barcodeImage)
        {
            container
                .PaddingTop(5)
                .Column(column =>
                {
                    column.Spacing(2);

                    column.Item()
                        .PaddingHorizontal(3)
                        .Image(barcodeImage)
                        .FitWidth();

                    column.Item()
                        .AlignCenter()
                        .Text(barcodeValue)
                        .FontSize(6.2f)
                        .FontColor(
                            Colors.Grey.Darken1);
                });
        }

        private static void ComposeFooter(
            IContainer container,
            ReceiptSnapshot snapshot)
        {
            var localDate =
                ToLocal(snapshot.SaleDateUtc);

            container
                .PaddingTop(4)
                .AlignCenter()
                .Column(column =>
                {
                    column.Spacing(2);

                    if (!string.IsNullOrWhiteSpace(
                            snapshot.FooterText))
                    {
                        column.Item()
                            .AlignCenter()
                            .Text(
                                snapshot.FooterText)
                            .FontSize(7.2f)
                            .SemiBold();
                    }

                    //column.Item()
                    //    .AlignCenter()
                    //    .Text("Merci pour votre achat.")
                    //    .FontSize(7.5f);

                    column.Item()
                        .PaddingTop(3)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .AlignLeft()
                                .Text(
                                    localDate.ToString(
                                        "dd/MM/yyyy",
                                        ReceiptCulture))
                                .FontSize(7);

                            row.RelativeItem()
                                .AlignRight()
                                .Text(
                                    localDate.ToString(
                                        "HH:mm:ss",
                                        ReceiptCulture))
                                .FontSize(7);
                        });

                    if (!string.IsNullOrWhiteSpace(
                            snapshot.SocialLine))
                    {
                        column.Item()
                            .PaddingTop(4)
                            .AlignCenter()
                            .Text("JOIN US TODAY")
                            .SemiBold()
                            .FontSize(7.6f);

                        column.Item()
                            .AlignCenter()
                            .Text(
                                snapshot.SocialLine)
                            .SemiBold()
                            .FontSize(7.6f);
                    }
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
                        .Text(left)
                        .FontSize(fontSize);

                var rightText =
                    row.ConstantItem(100)
                        .AlignRight()
                        .Text(right)
                        .FontSize(fontSize);

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
                .PaddingVertical(2)
                .LineHorizontal(0.5f)
                .LineColor(Colors.Grey.Medium);
        }

        private static void AddCenteredText(
            ColumnDescriptor column,
            string? value,
            float fontSize)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            foreach (var line in value
                         .Replace("\r", string.Empty)
                         .Split(
                             '\n',
                             StringSplitOptions.RemoveEmptyEntries |
                             StringSplitOptions.TrimEntries))
            {
                column.Item()
                    .AlignCenter()
                    .Text(line)
                    .FontSize(fontSize);
            }
        }

        private static string TranslatePaymentMethod(
            string? method)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                return "Paiement";
            }

            return method.Trim().ToLowerInvariant() switch
            {
                "cash" => "Espèces",
                "card" => "Carte",
                "credit" => "Crédit",
                "banktransfer" => "Virement",
                "bank transfer" => "Virement",
                _ => method.Trim()
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

        private static string ResolveCurrencyCode(
            ReceiptSnapshot snapshot)
        {
            return string.IsNullOrWhiteSpace(
                    snapshot.CurrencyCode)
                ? "EUR"
                : snapshot.CurrencyCode
                    .Trim()
                    .ToUpperInvariant();
        }

        private static string FormatMoney(
            decimal amount,
            string currencyCode)
        {
            return
                $"{FormatAmountNoSymbol(amount)} " +
                $"{currencyCode}";
        }

        private static string FormatAmountNoSymbol(
            decimal amount)
        {
            return amount.ToString(
                "0.00",
                ReceiptCulture);
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