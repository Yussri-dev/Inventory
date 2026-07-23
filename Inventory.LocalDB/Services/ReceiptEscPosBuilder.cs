using Inventory.LocalDB.Models;
using System.Globalization;
using System.Text;

namespace Inventory.LocalDB.Services
{
    public static class ReceiptEscPosBuilder
    {
        private static readonly CultureInfo ReceiptCulture =
            CultureInfo.GetCultureInfo(
                "fr-BE");

        public static byte[] Build(
            ReceiptPrintDocument document,
            ReceiptPrinterOptions options)
        {
            ArgumentNullException.ThrowIfNull(
                document);

            ArgumentNullException.ThrowIfNull(
                options);

            Encoding.RegisterProvider(
                CodePagesEncodingProvider.Instance);

            var encoding =
                ResolveEncoding(
                    options.CodePage);

            var width =
                Math.Clamp(
                    options.CharactersPerLine,
                    24,
                    64);

            using var stream =
                new MemoryStream();

            var writer =
                new EscPosWriter(
                    stream,
                    encoding,
                    width);

            var snapshot =
                document.Snapshot;

            writer.Initialize();

            /*
             * Certaines imprimantes utilisent ESC t n pour sélectionner
             * la table de caractères.
             *
             * La valeur exacte dépend de l'imprimante. Le texte reste
             * néanmoins encodé avec la code page configurée.
             */
            writer.SetCodePage(
                options.CodePage);

            PrintHeader(
                writer,
                document,
                options);

            PrintSaleInformation(
                writer,
                snapshot);

            PrintLines(
                writer,
                snapshot);

            PrintTotals(
                writer,
                snapshot);

            PrintVatSummary(
                writer,
                snapshot);

            PrintPayments(
                writer,
                snapshot);

            PrintFooter(
                writer,
                snapshot);

            writer.FeedLines(
                Math.Max(
                    1,
                    options.FeedLinesAfterReceipt));

            if (options.CutPaper)
            {
                writer.CutPaper();
            }

            return stream.ToArray();
        }

        private static void PrintHeader(
            EscPosWriter writer,
            ReceiptPrintDocument document,
            ReceiptPrinterOptions options)
        {
            var snapshot =
                document.Snapshot;

            writer.AlignCenter();
            writer.BoldOn();
            writer.DoubleSizeOn();

            writer.WriteWrappedLine(
                snapshot.CompanyName);

            writer.DoubleSizeOff();
            writer.BoldOff();

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CompanyAddress))
            {
                writer.WriteWrappedLine(
                    snapshot.CompanyAddress);
            }

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CompanyPhone))
            {
                writer.WriteWrappedLine(
                    $"Tél. {snapshot.CompanyPhone}");
            }

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CompanyEmail))
            {
                writer.WriteWrappedLine(
                    snapshot.CompanyEmail);
            }

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CompanyTaxNumber))
            {
                writer.WriteWrappedLine(
                    $"TVA : {snapshot.CompanyTaxNumber}");
            }

            writer.WriteLine();

            writer.BoldOn();

            writer.WriteWrappedLine(
                options.ReceiptTitle);

            writer.BoldOff();

            if (document.IsDuplicate)
            {
                writer.WriteLine();

                writer.BoldOn();
                writer.DoubleSizeOn();

                writer.WriteLine(
                    "DUPLICATA");

                writer.DoubleSizeOff();
                writer.BoldOff();

                writer.WriteWrappedLine(
                    $"Copie n° {document.CopyNumber}");

                writer.WriteWrappedLine(
                    $"Réimprimé le " +
                    $"{ToLocal(document.PrintedAtUtc):dd/MM/yyyy HH:mm}");

                if (!string.IsNullOrWhiteSpace(
                        document.Reason))
                {
                    writer.WriteWrappedLine(
                        $"Motif : {document.Reason}");
                }
            }
            else
            {
                writer.WriteWrappedLine(
                    "ORIGINAL");
            }

            writer.WriteLine();
            writer.AlignLeft();
            writer.WriteSeparator();
        }

        private static void PrintSaleInformation(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            writer.WriteTwoColumns(
                "Ticket",
                snapshot.InvoiceNumber);

            writer.WriteTwoColumns(
                "Date",
                ToLocal(
                    snapshot.SaleDateUtc)
                    .ToString(
                        "dd/MM/yyyy HH:mm",
                        ReceiptCulture));

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CashierName))
            {
                writer.WriteTwoColumns(
                    "Caissier",
                    snapshot.CashierName);
            }

            writer.WriteTwoColumns(
                "Client",
                string.IsNullOrWhiteSpace(
                    snapshot.CustomerName)
                        ? "Client comptoir"
                        : snapshot.CustomerName);

            writer.WriteSeparator();
        }

        private static void PrintLines(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            foreach (var line in snapshot.Lines)
            {
                writer.BoldOn();

                writer.WriteWrappedLine(
                    line.ProductName);

                writer.BoldOff();

                if (!string.IsNullOrWhiteSpace(
                        line.Barcode))
                {
                    writer.WriteWrappedLine(
                        $"  Code : {line.Barcode}");
                }

                var quantity =
                    FormatQuantity(
                        line.Quantity);

                var unitPrice =
                    FormatMoney(
                        line.UnitPrice);

                var lineTotal =
                    FormatMoney(
                        line.TotalInclVat);

                writer.WriteTwoColumns(
                    $"  {quantity} x {unitPrice}",
                    lineTotal);

                if (line.DiscountPercent > 0m)
                {
                    writer.WriteTwoColumns(
                        $"  Réduction {line.DiscountPercent:0.##} %",
                        $"-{FormatMoney(line.TotalDiscount)}");
                }
                else if (line.DiscountAmount > 0m)
                {
                    writer.WriteTwoColumns(
                        "  Réduction",
                        $"-{FormatMoney(line.TotalDiscount)}");
                }

                writer.WriteTwoColumns(
                    $"  TVA {line.VatRate:0.##} %",
                    FormatMoney(
                        line.VatAmount));
            }

            writer.WriteSeparator();
        }

        private static void PrintTotals(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            writer.WriteTwoColumns(
                "Total HT",
                FormatMoney(
                    snapshot.SubtotalExclVat));

            writer.WriteTwoColumns(
                "TVA",
                FormatMoney(
                    snapshot.TotalVat));

            writer.WriteLine();

            writer.BoldOn();
            writer.DoubleSizeOn();

            writer.WriteTwoColumns(
                "TOTAL",
                FormatMoney(
                    snapshot.TotalAmount));

            writer.DoubleSizeOff();
            writer.BoldOff();

            writer.WriteSeparator();
        }

        private static void PrintVatSummary(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            if (snapshot.VatSummary.Count == 0)
            {
                return;
            }

            writer.BoldOn();

            writer.WriteLine(
                "DÉTAIL TVA");

            writer.BoldOff();

            foreach (var vat in snapshot.VatSummary)
            {
                writer.WriteWrappedLine(
                    $"TVA {vat.VatRate:0.##}%");

                writer.WriteTwoColumns(
                    "  Base HT",
                    FormatMoney(
                        vat.AmountExclVat));

                writer.WriteTwoColumns(
                    "  TVA",
                    FormatMoney(
                        vat.VatAmount));

                writer.WriteTwoColumns(
                    "  TTC",
                    FormatMoney(
                        vat.AmountInclVat));
            }

            writer.WriteSeparator();
        }

        private static void PrintPayments(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            writer.BoldOn();

            writer.WriteLine(
                "PAIEMENTS");

            writer.BoldOff();

            foreach (var payment in snapshot.Payments)
            {
                writer.WriteTwoColumns(
                    TranslatePaymentMethod(
                        payment.Method),
                    FormatMoney(
                        payment.Amount));
            }

            if (snapshot.ChangeAmount > 0m)
            {
                writer.BoldOn();

                writer.WriteTwoColumns(
                    "MONNAIE",
                    FormatMoney(
                        snapshot.ChangeAmount));

                writer.BoldOff();
            }

            writer.WriteSeparator();
        }

        private static void PrintFooter(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            writer.AlignCenter();

            if (!string.IsNullOrWhiteSpace(
                    snapshot.FooterText))
            {
                writer.WriteLine();

                writer.WriteWrappedLine(
                    snapshot.FooterText);
            }

            writer.WriteLine();

            writer.WriteWrappedLine(
                "Merci pour votre achat.");

            writer.AlignLeft();
        }

        private static Encoding ResolveEncoding(
            int codePage)
        {
            try
            {
                return Encoding.GetEncoding(
                    codePage,
                    EncoderFallback.ReplacementFallback,
                    DecoderFallback.ReplacementFallback);
            }
            catch
            {
                return Encoding.GetEncoding(
                    858,
                    EncoderFallback.ReplacementFallback,
                    DecoderFallback.ReplacementFallback);
            }
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
                    method
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
            return $"€{amount.ToString("N2", ReceiptCulture)}";
        }

        private static string FormatQuantity(
            decimal quantity)
        {
            return quantity.ToString(
                "0.###",
                ReceiptCulture);
        }

        private sealed class EscPosWriter
        {
            private readonly Stream _stream;
            private readonly Encoding _encoding;
            private readonly int _width;

            public EscPosWriter(
                Stream stream,
                Encoding encoding,
                int width)
            {
                _stream =
                    stream;

                _encoding =
                    encoding;

                _width =
                    width;
            }

            public void Initialize()
            {
                WriteBytes(
                    0x1B,
                    0x40);
            }

            public void SetCodePage(
                int codePage)
            {
                /*
                 * ESC/POS utilise un index interne et non toujours le
                 * numéro Windows de la code page.
                 *
                 * CP858 correspond souvent à l'index 19 sur Epson.
                 */
                var escPosIndex =
                    codePage switch
                    {
                        437 => 0,
                        850 => 2,
                        858 => 19,
                        _ => 19
                    };

                WriteBytes(
                    0x1B,
                    0x74,
                    (byte)escPosIndex);
            }

            public void AlignLeft()
            {
                WriteBytes(
                    0x1B,
                    0x61,
                    0x00);
            }

            public void AlignCenter()
            {
                WriteBytes(
                    0x1B,
                    0x61,
                    0x01);
            }

            public void BoldOn()
            {
                WriteBytes(
                    0x1B,
                    0x45,
                    0x01);
            }

            public void BoldOff()
            {
                WriteBytes(
                    0x1B,
                    0x45,
                    0x00);
            }

            public void DoubleSizeOn()
            {
                WriteBytes(
                    0x1D,
                    0x21,
                    0x11);
            }

            public void DoubleSizeOff()
            {
                WriteBytes(
                    0x1D,
                    0x21,
                    0x00);
            }

            public void WriteLine(
                string? value = null)
            {
                if (!string.IsNullOrEmpty(
                        value))
                {
                    WriteText(
                        value);
                }

                WriteBytes(
                    0x0A);
            }

            public void WriteWrappedLine(
                string? value)
            {
                if (string.IsNullOrWhiteSpace(
                        value))
                {
                    return;
                }

                foreach (var line in Wrap(
                             Clean(value),
                             _width))
                {
                    WriteLine(
                        line);
                }
            }

            public void WriteTwoColumns(
                string? left,
                string? right)
            {
                var cleanLeft =
                    Clean(
                        left);

                var cleanRight =
                    Clean(
                        right);

                if (string.IsNullOrEmpty(
                        cleanRight))
                {
                    WriteWrappedLine(
                        cleanLeft);

                    return;
                }

                var minimumSpaces =
                    1;

                var availableForLeft =
                    _width -
                    cleanRight.Length -
                    minimumSpaces;

                if (availableForLeft <= 0)
                {
                    WriteWrappedLine(
                        cleanLeft);

                    WriteLine(
                        cleanRight.PadLeft(
                            Math.Min(
                                _width,
                                cleanRight.Length)));

                    return;
                }

                var wrappedLeft =
                    Wrap(
                        cleanLeft,
                        availableForLeft)
                        .ToList();

                if (wrappedLeft.Count == 0)
                {
                    wrappedLeft.Add(
                        string.Empty);
                }

                for (var index = 0;
                     index < wrappedLeft.Count - 1;
                     index++)
                {
                    WriteLine(
                        wrappedLeft[index]);
                }

                var lastLeft =
                    wrappedLeft[^1];

                var spaces =
                    Math.Max(
                        minimumSpaces,
                        _width -
                        lastLeft.Length -
                        cleanRight.Length);

                WriteLine(
                    lastLeft +
                    new string(
                        ' ',
                        spaces) +
                    cleanRight);
            }

            public void WriteSeparator()
            {
                WriteLine(
                    new string(
                        '-',
                        _width));
            }

            public void FeedLines(
                int lineCount)
            {
                for (var index = 0;
                     index < lineCount;
                     index++)
                {
                    WriteLine();
                }
            }

            public void CutPaper()
            {
                /*
                 * GS V 66 0 : coupe partielle.
                 */
                WriteBytes(
                    0x1D,
                    0x56,
                    0x42,
                    0x00);
            }

            private void WriteText(
                string value)
            {
                var bytes =
                    _encoding.GetBytes(
                        value);

                _stream.Write(
                    bytes,
                    0,
                    bytes.Length);
            }

            private void WriteBytes(
                params byte[] bytes)
            {
                _stream.Write(
                    bytes,
                    0,
                    bytes.Length);
            }

            private static string Clean(
                string? value)
            {
                return string.IsNullOrWhiteSpace(
                        value)
                    ? string.Empty
                    : value
                        .Replace(
                            "\r",
                            " ")
                        .Replace(
                            "\n",
                            " ")
                        .Trim();
            }

            private static IEnumerable<string> Wrap(
                string value,
                int width)
            {
                if (string.IsNullOrEmpty(
                        value))
                {
                    yield return string.Empty;
                    yield break;
                }

                var remaining =
                    value;

                while (remaining.Length >
                       width)
                {
                    var breakIndex =
                        remaining.LastIndexOf(
                            ' ',
                            width);

                    if (breakIndex <= 0)
                    {
                        breakIndex =
                            width;
                    }

                    yield return remaining[..breakIndex]
                        .TrimEnd();

                    remaining =
                        remaining[breakIndex..]
                            .TrimStart();
                }

                if (remaining.Length > 0)
                {
                    yield return remaining;
                }
            }
        }
    }
}
