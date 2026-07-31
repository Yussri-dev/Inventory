using Inventory.LocalDB.Services.Interfaces;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace Inventory.LocalDB.Services
{
    public sealed class ReceiptBarcodeGenerator
        : IReceiptBarcodeGenerator
    {
        public byte[] GenerateCode128Png(
            string value,
            int width = 320,
            int height = 80)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "The barcode value is required.",
                    nameof(value));
            }

            var writer =
                new BarcodeWriter
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Width = width,
                        Height = height,
                        Margin = 2,
                        PureBarcode = true
                    }
                };

            using var bitmap =
                writer.Write(value);

            using var image =
                SKImage.FromBitmap(bitmap);

            using var data =
                image.Encode(
                    SKEncodedImageFormat.Png,
                    100);

            return data.ToArray();
        }
    }
}
