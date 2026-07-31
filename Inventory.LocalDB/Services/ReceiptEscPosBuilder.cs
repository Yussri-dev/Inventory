using Inventory.LocalDB.Models;
using SkiaSharp;
using System.Globalization;
using System.Text;

namespace Inventory.LocalDB.Services
{
    public static class ReceiptEscPosBuilder
    {
        private static readonly CultureInfo ReceiptCulture =
            CultureInfo.GetCultureInfo("fr-BE");

        public static byte[] Build(
            ReceiptPrintDocument document,
            ReceiptPrinterOptions options)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(document.Snapshot);

            Encoding.RegisterProvider(
                CodePagesEncodingProvider.Instance);

            var encoding =
                ResolveEncoding(options.CodePage);

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
            writer.SetCodePage(options.CodePage);

            PrintHeader(
                writer,
                document);

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

            PrintBarcodeSection(
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
            ReceiptPrintDocument document)
        {
            var snapshot =
                document.Snapshot;

            writer.AlignCenter();

            TryPrintLogo(
                writer,
                snapshot);

            writer.BoldOn();
            writer.DoubleSizeOn();
            writer.WriteWrappedLine(
                snapshot.CompanyName);
            writer.DoubleSizeOff();
            writer.BoldOff();

            if (!string.IsNullOrWhiteSpace(
                    snapshot.HeaderTagLine))
            {
                writer.WriteWrappedLine(
                    snapshot.HeaderTagLine);
            }

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CompanyAddress))
            {
                writer.WriteWrappedLine(
                    snapshot.CompanyAddress);
            }

            if (!string.IsNullOrWhiteSpace(
                    snapshot.ExtraAddressLine))
            {
                writer.WriteWrappedLine(
                    snapshot.ExtraAddressLine);
            }

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CompanyPhone))
            {
                writer.WriteWrappedLine(
                    $"NUMBER : {snapshot.CompanyPhone}");
            }

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CompanyEmail))
            {
                writer.WriteWrappedLine(
                    snapshot.CompanyEmail);
            }

            if (!string.IsNullOrWhiteSpace(
                    snapshot.SocialLine))
            {
                writer.WriteWrappedLine(
                    snapshot.SocialLine);
            }

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CompanyTaxNumber))
            {
                writer.WriteWrappedLine(
                    $"TVA : {snapshot.CompanyTaxNumber}");
            }

            if (document.IsDuplicate)
            {
                writer.WriteLine();
                writer.BoldOn();
                writer.DoubleSizeOn();
                writer.WriteWrappedLine("DUPLICATA");
                writer.DoubleSizeOff();
                writer.BoldOff();

                writer.WriteWrappedLine(
                    $"COPIE N° {document.CopyNumber}");

                writer.WriteWrappedLine(
                    $"RÉIMPRIMÉ LE " +
                    $"{ToLocal(document.PrintedAtUtc):dd/MM/yyyy HH:mm}");

                if (!string.IsNullOrWhiteSpace(
                        document.Reason))
                {
                    writer.WriteWrappedLine(
                        $"MOTIF : {document.Reason}");
                }
            }

            writer.WriteLine();
            writer.AlignLeft();
        }

        private static void PrintSaleInformation(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            var localDate =
                ToLocal(snapshot.SaleDateUtc);

            writer.WriteLabelValue(
                "N° TICKET",
                snapshot.InvoiceNumber);

            writer.WriteLabelValue(
                "CUSTOMER",
                string.IsNullOrWhiteSpace(
                    snapshot.CustomerName)
                        ? "CLIENT PASSAGER"
                        : snapshot.CustomerName);

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CashierName))
            {
                writer.WriteLabelValue(
                    "CASHIER",
                    snapshot.CashierName);
            }

            writer.WriteTwoColumns(
                "DATE",
                localDate.ToString(
                    "dd/MM/yyyy HH:mm",
                    ReceiptCulture));

            writer.WriteLine();
        }

        private static void PrintLines(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            writer.BoldOn();
            writer.WriteItemTableHeader();
            writer.BoldOff();

            foreach (var line in snapshot.Lines)
            {
                writer.WriteItemLine(
                    line.ProductName,
                    FormatQuantity(line.Quantity),
                    FormatAmountNoSymbol(line.UnitPrice),
                    FormatAmountNoSymbol(line.TotalInclVat));

                if (!string.IsNullOrWhiteSpace(
                        line.Barcode))
                {
                    writer.WriteWrappedLine(
                        $"  CODE : {line.Barcode}");
                }

                if (line.DiscountPercent > 0m)
                {
                    writer.WriteTwoColumns(
                        $"  REDUCTION {line.DiscountPercent:0.##}%",
                        $"-{FormatAmountNoSymbol(line.TotalDiscount)}");
                }
                else if (line.DiscountAmount > 0m)
                {
                    writer.WriteTwoColumns(
                        "  REDUCTION",
                        $"-{FormatAmountNoSymbol(line.TotalDiscount)}");
                }

                writer.WriteLine();
            }

            writer.WriteTwoColumns(
                $"ROW NUMBER : {snapshot.Lines.Count}",
                $"TOTAL QTY : " +
                snapshot.Lines
                    .Sum(item => item.Quantity)
                    .ToString(
                        "0.###",
                        ReceiptCulture));
        }

        private static void PrintTotals(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            var currencyCode =
                NormalizeCurrencyCode(
                    snapshot.CurrencyCode);

            var receivedAmount =
                snapshot.TotalReceived > 0m
                    ? snapshot.TotalReceived
                    : RoundMoney(
                        snapshot.Payments?.Sum(
                            payment => payment.Amount) ?? 0m);

            writer.WriteLine();
            writer.BoldOn();
            writer.DoubleHeightOn();

            writer.WriteTwoColumns(
                "TOTAL",
                $"{FormatAmountNoSymbol(snapshot.TotalAmount)} " +
                currencyCode);

            writer.DoubleSizeOff();
            writer.BoldOff();

            writer.WriteLine();

            writer.WriteTwoColumns(
                "TOTAL HT",
                $"{FormatAmountNoSymbol(snapshot.SubtotalExclVat)} " +
                currencyCode);

            writer.WriteTwoColumns(
                "TVA",
                $"{FormatAmountNoSymbol(snapshot.TotalVat)} " +
                currencyCode);

            writer.WriteLine();

            writer.WriteTwoColumns(
                "REÇU",
                $"{FormatAmountNoSymbol(receivedAmount)} " +
                currencyCode);

            writer.WriteTwoColumns(
                "MONNAIE",
                $"{FormatAmountNoSymbol(snapshot.ChangeAmount)} " +
                currencyCode);
        }

        private static void PrintVatSummary(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            if (snapshot.VatSummary == null ||
                snapshot.VatSummary.Count == 0)
            {
                return;
            }

            var currencyCode =
                NormalizeCurrencyCode(
                    snapshot.CurrencyCode);

            writer.WriteLine();
            writer.BoldOn();
            writer.WriteLine("DÉTAIL TVA");
            writer.BoldOff();

            foreach (var vat in snapshot.VatSummary)
            {
                writer.WriteTwoColumns(
                    $"TVA {vat.VatRate:0.##}%",
                    $"{FormatAmountNoSymbol(vat.VatAmount)} " +
                    currencyCode);

                writer.WriteTwoColumns(
                    "  BASE HT",
                    $"{FormatAmountNoSymbol(vat.AmountExclVat)} " +
                    currencyCode);
            }
        }

        private static void PrintPayments(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            if (snapshot.Payments == null ||
                snapshot.Payments.Count == 0)
            {
                return;
            }

            var currencyCode =
                NormalizeCurrencyCode(
                    snapshot.CurrencyCode);

            writer.WriteLine();
            writer.BoldOn();
            writer.WriteLine("PAIEMENTS");
            writer.BoldOff();

            foreach (var payment in snapshot.Payments)
            {
                writer.WriteTwoColumns(
                    TranslatePaymentMethod(
                        payment.Method),
                    $"{FormatAmountNoSymbol(payment.Amount)} " +
                    currencyCode);
            }
        }

        private static void PrintBarcodeSection(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(
                    snapshot.BarcodeValue))
            {
                return;
            }

            writer.WriteLine();
            writer.AlignCenter();

            writer.WriteCode128(
                snapshot.BarcodeValue);

            writer.WriteWrappedLine(
                snapshot.BarcodeValue);
        }

        private static void PrintFooter(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            var localDate =
                ToLocal(snapshot.SaleDateUtc);

            writer.WriteLine();
            writer.AlignCenter();

            if (!string.IsNullOrWhiteSpace(
                    snapshot.FooterText))
            {
                writer.WriteWrappedLine(
                    snapshot.FooterText);

                writer.WriteLine();
            }

            writer.AlignLeft();
            writer.WriteTwoColumns(
                localDate.ToString(
                    "dd/MM/yyyy",
                    ReceiptCulture),
                localDate.ToString(
                    "HH:mm:ss",
                    ReceiptCulture));

            writer.WriteLine();
            writer.AlignCenter();

            if (!string.IsNullOrWhiteSpace(
                    snapshot.SocialLine))
            {
                writer.BoldOn();
                writer.WriteWrappedLine("JOIN US TODAY");
                writer.BoldOff();
                writer.WriteWrappedLine(
                    snapshot.SocialLine);
            }
            else
            {
                writer.BoldOn();
                writer.WriteWrappedLine("MERCI POUR VOTRE ACHAT");
                writer.BoldOff();
            }

            writer.AlignLeft();
        }

        private static void TryPrintLogo(
            EscPosWriter writer,
            ReceiptSnapshot snapshot)
        {
            if (snapshot.LogoBytes == null ||
                snapshot.LogoBytes.Length == 0)
            {
                return;
            }

            try
            {
                writer.WriteRasterImage(
                    snapshot.LogoBytes,
                    maxWidth: 256);

                writer.WriteLine();
            }
            catch
            {
                /*
                 * Un logo invalide ne doit pas empêcher l'impression
                 * du ticket. Le nom du magasin reste affiché en texte.
                 */
            }
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
            if (string.IsNullOrWhiteSpace(method))
            {
                return "PAIEMENT";
            }

            return method.Trim().ToLowerInvariant() switch
            {
                "cash" => "ESPÈCES",
                "card" => "CARTE",
                "credit" => "CRÉDIT",
                "banktransfer" => "VIREMENT",
                "bank transfer" => "VIREMENT",
                _ => method.Trim().ToUpperInvariant()
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

        private static string FormatQuantity(
            decimal quantity)
        {
            return quantity.ToString(
                "0.###",
                ReceiptCulture);
        }

        private static string FormatAmountNoSymbol(
            decimal amount)
        {
            return amount.ToString(
                "0.00",
                ReceiptCulture);
        }

        private static string NormalizeCurrencyCode(
            string? currencyCode)
        {
            return string.IsNullOrWhiteSpace(currencyCode)
                ? "EUR"
                : currencyCode.Trim().ToUpperInvariant();
        }

        private static decimal RoundMoney(
            decimal amount)
        {
            return Math.Round(
                amount,
                2,
                MidpointRounding.AwayFromZero);
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

            public void DoubleHeightOn()
            {
                WriteBytes(
                    0x1D,
                    0x21,
                    0x01);
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
                if (!string.IsNullOrEmpty(value))
                {
                    WriteText(value);
                }

                WriteBytes(0x0A);
            }

            public void WriteWrappedLine(
                string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                foreach (var line in Wrap(
                             Clean(value),
                             _width))
                {
                    WriteLine(line);
                }
            }

            public void WriteLabelValue(
                string label,
                string? value)
            {
                var cleanLabel =
                    Clean(label);

                var cleanValue =
                    Clean(value);

                if (string.IsNullOrWhiteSpace(cleanValue))
                {
                    WriteLine($"{cleanLabel} :");
                    return;
                }

                WriteWrappedLine(
                    $"{cleanLabel} : {cleanValue}");
            }

            public void WriteTwoColumns(
                string? left,
                string? right)
            {
                var cleanLeft =
                    Clean(left);

                var cleanRight =
                    Clean(right);

                if (string.IsNullOrEmpty(cleanRight))
                {
                    WriteWrappedLine(cleanLeft);
                    return;
                }

                var availableForLeft =
                    _width -
                    cleanRight.Length -
                    1;

                if (availableForLeft <= 0)
                {
                    WriteWrappedLine(cleanLeft);
                    WriteLine(
                        cleanRight.Length <= _width
                            ? cleanRight.PadLeft(_width)
                            : cleanRight);
                    return;
                }

                var wrappedLeft =
                    Wrap(
                            cleanLeft,
                            availableForLeft)
                        .ToList();

                if (wrappedLeft.Count == 0)
                {
                    wrappedLeft.Add(string.Empty);
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
                        1,
                        _width -
                        lastLeft.Length -
                        cleanRight.Length);

                WriteLine(
                    lastLeft +
                    new string(' ', spaces) +
                    cleanRight);
            }

            public void WriteItemTableHeader()
            {
                var quantityWidth =
                    _width >= 42
                        ? 5
                        : 4;

                var priceWidth =
                    _width >= 42
                        ? 9
                        : 7;

                var amountWidth =
                    _width >= 42
                        ? 10
                        : 8;

                var designationWidth =
                    Math.Max(
                        8,
                        _width -
                        quantityWidth -
                        priceWidth -
                        amountWidth -
                        3);

                WriteLine(
                    "DESIGNATION".PadRight(designationWidth) +
                    " " +
                    "QTE".PadLeft(quantityWidth) +
                    " " +
                    "PRICE".PadLeft(priceWidth) +
                    " " +
                    "AMOUNT".PadLeft(amountWidth));
            }

            public void WriteItemLine(
                string? designation,
                string quantity,
                string price,
                string amount)
            {
                var quantityWidth =
                    _width >= 42
                        ? 5
                        : 4;

                var priceWidth =
                    _width >= 42
                        ? 9
                        : 7;

                var amountWidth =
                    _width >= 42
                        ? 10
                        : 8;

                var designationWidth =
                    Math.Max(
                        8,
                        _width -
                        quantityWidth -
                        priceWidth -
                        amountWidth -
                        3);

                var designationLines =
                    Wrap(
                            Clean(designation),
                            designationWidth)
                        .ToList();

                if (designationLines.Count == 0)
                {
                    designationLines.Add(string.Empty);
                }

                for (var index = 0;
                     index < designationLines.Count;
                     index++)
                {
                    var currentDesignation =
                        designationLines[index]
                            .PadRight(designationWidth);

                    if (index == designationLines.Count - 1)
                    {
                        WriteLine(
                            currentDesignation +
                            " " +
                            TrimToWidth(quantity, quantityWidth)
                                .PadLeft(quantityWidth) +
                            " " +
                            TrimToWidth(price, priceWidth)
                                .PadLeft(priceWidth) +
                            " " +
                            TrimToWidth(amount, amountWidth)
                                .PadLeft(amountWidth));
                    }
                    else
                    {
                        WriteLine(currentDesignation);
                    }
                }
            }

            public void WriteCode128(
                string value)
            {
                var cleanValue =
                    Clean(value);

                if (string.IsNullOrWhiteSpace(cleanValue))
                {
                    return;
                }

                var payload =
                    BuildCode128Payload(cleanValue);

                if (payload.Length == 0 ||
                    payload.Length > byte.MaxValue)
                {
                    throw new InvalidOperationException(
                        "The receipt barcode is too long for ESC/POS CODE128.");
                }

                /* HRI désactivé : la valeur est imprimée juste en dessous. */
                WriteBytes(
                    0x1D,
                    0x48,
                    0x00);

                WriteBytes(
                    0x1D,
                    0x68,
                    70);

                WriteBytes(
                    0x1D,
                    0x77,
                    0x02);

                WriteBytes(
                    0x1D,
                    0x6B,
                    73,
                    (byte)payload.Length);

                WriteRaw(payload);
                WriteLine();
            }

            [Obsolete]
            public void WriteRasterImage(
                byte[] imageBytes,
                int maxWidth = 256)
            {
                ArgumentNullException.ThrowIfNull(
                    imageBytes);

                if (imageBytes.Length == 0)
                {
                    return;
                }

                using var original =
                    SKBitmap.Decode(imageBytes)
                    ?? throw new InvalidOperationException(
                        "The receipt logo could not be decoded.");

                var targetWidth =
                    Math.Clamp(
                        Math.Min(maxWidth, original.Width),
                        1,
                        maxWidth);

                var targetHeight =
                    Math.Max(
                        1,
                        (int)Math.Round(
                            original.Height *
                            (targetWidth / (double)original.Width)));

                using var resized =
                    new SKBitmap(
                        new SKImageInfo(
                            targetWidth,
                            targetHeight,
                            SKColorType.Bgra8888,
                            SKAlphaType.Premul));

                using (var canvas =
                       new SKCanvas(resized))
                using (var paint =
                       new SKPaint
                       {
                           IsAntialias = true,
                           //FilterQuality = SKFilterQuality.High
                       })
                {
                    canvas.Clear(SKColors.White);

                    canvas.DrawBitmap(
                        original,
                        new SKRect(
                            0,
                            0,
                            targetWidth,
                            targetHeight),
                        paint);
                }

                var widthBytes =
                    (targetWidth + 7) / 8;

                var raster =
                    new byte[
                        widthBytes *
                        targetHeight];

                for (var y = 0;
                     y < targetHeight;
                     y++)
                {
                    for (var x = 0;
                         x < targetWidth;
                         x++)
                    {
                        var color =
                            resized.GetPixel(x, y);

                        var alpha =
                            color.Alpha / 255d;

                        var luminance =
                            (
                                0.299d * color.Red +
                                0.587d * color.Green +
                                0.114d * color.Blue
                            ) * alpha +
                            255d * (1d - alpha);

                        if (luminance >= 170d)
                        {
                            continue;
                        }

                        var byteIndex =
                            y * widthBytes +
                            x / 8;

                        raster[byteIndex] |=
                            (byte)(0x80 >> (x % 8));
                    }
                }

                var xLow =
                    (byte)(widthBytes & 0xFF);

                var xHigh =
                    (byte)((widthBytes >> 8) & 0xFF);

                var yLow =
                    (byte)(targetHeight & 0xFF);

                var yHigh =
                    (byte)((targetHeight >> 8) & 0xFF);

                /* GS v 0 : image raster noir et blanc. */
                WriteBytes(
                    0x1D,
                    0x76,
                    0x30,
                    0x00,
                    xLow,
                    xHigh,
                    yLow,
                    yHigh);

                WriteRaw(raster);
                WriteLine();
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
                WriteBytes(
                    0x1D,
                    0x56,
                    0x42,
                    0x00);
            }

            private byte[] BuildCode128Payload(
                string value)
            {
                using var payload =
                    new MemoryStream();

                if (value.All(char.IsDigit) &&
                    value.Length >= 2)
                {
                    WriteAscii(payload, "{C");

                    var evenLength =
                        value.Length -
                        value.Length % 2;

                    WriteAscii(
                        payload,
                        value[..evenLength]);

                    if (evenLength < value.Length)
                    {
                        WriteAscii(payload, "{B");
                        WriteAscii(
                            payload,
                            value[evenLength..]);
                    }

                    return payload.ToArray();
                }

                WriteAscii(payload, "{B");

                foreach (var character in value)
                {
                    if (character == '{')
                    {
                        WriteAscii(payload, "{{");
                    }
                    else
                    {
                        var bytes =
                            _encoding.GetBytes(
                                character.ToString());

                        payload.Write(
                            bytes,
                            0,
                            bytes.Length);
                    }
                }

                return payload.ToArray();
            }

            private static void WriteAscii(
                Stream destination,
                string value)
            {
                var bytes =
                    Encoding.ASCII.GetBytes(value);

                destination.Write(
                    bytes,
                    0,
                    bytes.Length);
            }

            private void WriteText(
                string value)
            {
                var bytes =
                    _encoding.GetBytes(value);

                WriteRaw(bytes);
            }

            private void WriteRaw(
                byte[] bytes)
            {
                _stream.Write(
                    bytes,
                    0,
                    bytes.Length);
            }

            private void WriteBytes(
                params byte[] bytes)
            {
                WriteRaw(bytes);
            }

            private static string TrimToWidth(
                string value,
                int width)
            {
                if (string.IsNullOrEmpty(value) ||
                    value.Length <= width)
                {
                    return value;
                }

                return value[^width..];
            }

            private static string Clean(
                string? value)
            {
                return string.IsNullOrWhiteSpace(value)
                    ? string.Empty
                    : value
                        .Replace("\r", " ")
                        .Replace("\n", " ")
                        .Trim();
            }

            private static IEnumerable<string> Wrap(
                string value,
                int width)
            {
                if (string.IsNullOrEmpty(value))
                {
                    yield return string.Empty;
                    yield break;
                }

                var remaining =
                    value;

                while (remaining.Length > width)
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