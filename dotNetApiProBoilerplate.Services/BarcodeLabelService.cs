using Inventory.Domain.Barcodes;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;

namespace Inventory.Services
{
    public class BarcodeLabelService
    {
        private readonly IRepository<Product> _productRepository;

        public BarcodeLabelService(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<byte[]> GenerateProductLabelAsync(Guid productId)
        {
            var product = await _productRepository.Query()
                            .Include(p => p.CatalogProduct)
                            .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new NotFoundException("Product", productId);

            var name = product.CatalogProduct.Name;
            var brand = product.CatalogProduct.Brand ?? "";
            var barcode = EanTools.Normalize(product.CatalogProduct.Barcode);
            var price = product.SalePrice;

            var euros = (int)Math.Floor(price);
            var cents = (int)Math.Round((price - euros) * 100);

            var barcodeImage = GenerateBarcodeImage(barcode);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // ~100 x 50 mm  →  283 x 142 pt
                    page.Size(283, 142);
                    page.Margin(6);
                    page.Background(Colors.White);

                    page.Content().Column(mainCol =>
                    {
                        // ── LIGNE HAUTE : nom + prix ────────────────────────────
                        mainCol.Item().Row(row =>
                        {
                            // Nom + marque (gauche)
                            row.RelativeItem().Column(left =>
                            {
                                left.Item()
                                    .Text(name)
                                    .FontSize(10)
                                    .Bold()
                                    .ClampLines(2);

                                if (!string.IsNullOrWhiteSpace(brand))
                                    left.Item()
                                        .Text(brand)
                                        .FontSize(7)
                                        .FontColor(Colors.Grey.Darken2);
                            });

                            // Prix (droite) – entiers gros + centimes petit
                            row.ConstantItem(72).AlignRight().AlignMiddle().Row(priceRow =>
                            {
                                priceRow.AutoItem().AlignBottom()
                                    .Text(euros.ToString())
                                    .FontSize(38)
                                    .Bold();

                                priceRow.AutoItem().AlignTop().PaddingTop(4).Column(sup =>
                                {
                                    sup.Item().Text($"{cents:00}")
                                        .FontSize(14)
                                        .Bold();

                                    sup.Item().Text("€")
                                        .FontSize(10)
                                        .Bold();
                                });
                            });
                        });

                        // ── SÉPARATEUR ───────────────────────────────────────────
                        mainCol.Item().PaddingVertical(3)
                            .LineHorizontal(0.5f)
                            .LineColor(Colors.Grey.Lighten1);

                        // ── CODE-BARRES (pleine largeur) ─────────────────────────
                        mainCol.Item()
                            .Height(48)
                            .Image(barcodeImage, ImageScaling.FitArea);

                        // ── NUMÉRO EAN EN DESSOUS ────────────────────────────────
                        mainCol.Item().AlignCenter()
                            .Text(barcode)
                            .FontSize(7)
                            .FontColor(Colors.Black);
                    });
                });
            });

            return pdf.GeneratePdf();
        }

        private byte[] GenerateBarcodeImage(string ean)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.EAN_13,
                Options = new EncodingOptions
                {
                    Height = 110,
                    Width = 350,
                    Margin = 2,
                    PureBarcode = true  // numéro affiché séparément sous le barcode
                }
            };

            var pixelData = writer.Write(ean);

            using var bitmap = new Bitmap(
                pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);

            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppRgb);

            System.Runtime.InteropServices.Marshal.Copy(
                pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);

            bitmap.UnlockBits(bitmapData);

            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
    }
}
