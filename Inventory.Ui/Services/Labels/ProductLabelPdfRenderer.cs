using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using ZXing;
using ZXing.Common;

using QColors = QuestPDF.Helpers.Colors;
namespace Inventory.Ui.Services.Labels
{
    public static class ProductLabelPdfRenderer
    {
        public static byte[] Generate(
            ProductLabelData product)
        {
            var barcode =
                EanTools.Normalize(
                    product.Barcode);

            var euros =
                (int)Math.Floor(
                    product.SalePrice);

            var cents =
                (int)Math.Round(
                    (product.SalePrice - euros) *
                    100m);

            if (cents == 100)
            {
                euros++;
                cents = 0;
            }

            var barcodeImage =
                GenerateBarcodeImage(
                    barcode);

            var pdf =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(283, 142);
                        page.Margin(6);
                        page.Background(
                            QColors.White);

                        page.Content()
                            .Column(column =>
                            {
                                column.Item()
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Column(left =>
                                            {
                                                left.Item()
                                                    .Text(product.Name)
                                                    .FontSize(10)
                                                    .Bold()
                                                    .ClampLines(2);

                                                if (
                                                    !string.IsNullOrWhiteSpace(
                                                        product.Brand))
                                                {
                                                    left.Item()
                                                        .Text(product.Brand)
                                                        .FontSize(7)
                                                        .FontColor(
                                                            QColors.Grey.Darken2);
                                                }
                                            });

                                        row.ConstantItem(72)
                                            .AlignRight()
                                            .AlignMiddle()
                                            .Row(priceRow =>
                                            {
                                                priceRow.AutoItem()
                                                    .AlignBottom()
                                                    .Text(euros.ToString())
                                                    .FontSize(38)
                                                    .Bold();

                                                priceRow.AutoItem()
                                                    .AlignTop()
                                                    .PaddingTop(4)
                                                    .Column(priceColumn =>
                                                    {
                                                        priceColumn.Item()
                                                            .Text($"{cents:00}")
                                                            .FontSize(14)
                                                            .Bold();

                                                        priceColumn.Item()
                                                            .Text("€")
                                                            .FontSize(10)
                                                            .Bold();
                                                    });
                                            });
                                    });

                                column.Item()
                                    .PaddingVertical(3)
                                    .LineHorizontal(0.5f)
                                    .LineColor(
                                        QColors.Grey.Lighten1);

                                column.Item()
                                    .Height(48)
                                    .Image(
                                        barcodeImage,
                                        ImageScaling.FitArea);

                                column.Item()
                                    .AlignCenter()
                                    .Text(barcode)
                                    .FontSize(7)
                                    .FontColor(
                                        QColors.Black);
                            });
                    });
                });

            return pdf.GeneratePdf();
        }

        private static byte[] GenerateBarcodeImage(
    string ean)
        {
            if (
                string.IsNullOrWhiteSpace(ean) ||
                ean.Length != 13 ||
                !ean.All(char.IsDigit))
            {
                throw new InvalidOperationException(
                    $"The barcode '{ean}' is not a valid EAN-13 barcode.");
            }

            var writer =
                new BarcodeWriterPixelData
                {
                    Format =
                        BarcodeFormat.EAN_13,

                    Options =
                        new EncodingOptions
                        {
                            Height = 110,
                            Width = 350,
                            Margin = 2,
                            PureBarcode = true
                        }
                };

            var pixelData =
                writer.Write(ean);

            using var bitmap =
                new SKBitmap(
                    pixelData.Width,
                    pixelData.Height,
                    SKColorType.Bgra8888,
                    SKAlphaType.Premul);

            var destination =
                bitmap.GetPixels();

            if (destination == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "The barcode bitmap memory could not be allocated.");
            }

            System.Runtime.InteropServices.Marshal.Copy(
                pixelData.Pixels,
                0,
                destination,
                pixelData.Pixels.Length);

            using var image =
                SKImage.FromBitmap(
                    bitmap);

            using var encodedImage =
                image.Encode(
                    SKEncodedImageFormat.Png,
                    100)
                ?? throw new InvalidOperationException(
                    "The barcode image could not be encoded.");

            return encodedImage.ToArray();
        }
    }

    public static class EanTools
    {
        public static string Normalize(string code)
        {
            code = new string(code.Where(char.IsDigit).ToArray());

            if (code.Length == 12)
                return code + CalculateChecksum(code);

            if (code.Length == 13)
            {
                var expected = CalculateChecksum(code[..12]);
                return code[..12] + expected;
            }

            return code;
        }

        private static int CalculateChecksum(string digits)
        {
            int sum = 0;

            for (int i = 0; i < digits.Length; i++)
            {
                int n = digits[i] - '0';
                sum += (i % 2 == 0) ? n : n * 3;
            }

            return (10 - (sum % 10)) % 10;
        }
    }
}
