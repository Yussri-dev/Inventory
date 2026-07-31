using Inventory.Dto.Sales.Results;
using Inventory.Services.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;

namespace Inventory.Services.Ticket
{
    public sealed class PdfTicketFormatter : ITicketFormatter
    {
        public byte[] Format(SaleTicketResult ticket)
        {
            var barcodeImage = GenerateBarcodeImage(ticket.InvoiceNumber);
            var isWalkin = string.IsNullOrWhiteSpace(ticket.CustomerName)
                        || ticket.CustomerName == "Walk-in customer";

            var addressLine = string.Join(", ", new[]
            {
                ticket.StoreAddress,
                string.IsNullOrWhiteSpace(ticket.StorePostalCode) && string.IsNullOrWhiteSpace(ticket.StoreCity)
                    ? null
                    : $"{ticket.StorePostalCode} {ticket.StoreCity}".Trim()
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(227, 800);
                    page.Margin(10);

                    page.Content().Column(col =>
                    {
                        col.Spacing(2);

                        // HEADER
                        if (!string.IsNullOrWhiteSpace(ticket.ReceiptHeader))
                        {
                            col.Item().AlignCenter().Text(ticket.ReceiptHeader)
                                .FontSize(7).FontColor(Colors.Grey.Darken2);
                        }

                        col.Item().AlignCenter().Text(ticket.StoreName.ToUpper())
                            .FontSize(15).Bold().FontFamily("Arial");

                        if (!string.IsNullOrWhiteSpace(addressLine))
                        {
                            col.Item().AlignCenter().Text(addressLine)
                                .FontSize(7).FontColor(Colors.Grey.Darken2);
                        }

                        if (!string.IsNullOrWhiteSpace(ticket.StorePhone))
                        {
                            col.Item().AlignCenter().Text($"Tél : {ticket.StorePhone}")
                                .FontSize(7).FontColor(Colors.Grey.Darken2);
                        }

                        if (!string.IsNullOrWhiteSpace(ticket.StoreTaxNumber))
                        {
                            col.Item().AlignCenter().Text($"TVA : {ticket.StoreTaxNumber}")
                                .FontSize(7).FontColor(Colors.Grey.Darken2);
                        }

                        col.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                        // INFOS
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"N° {ticket.InvoiceNumber}").FontSize(7).Bold();
                            row.RelativeItem().AlignRight()
                                .Text(ticket.SaleDate.ToString("dd/MM/yyyy HH:mm"))
                                .FontSize(7);
                        });

                        col.Item().Text($"Client : {(isWalkin ? "Passage" : ticket.CustomerName)}")
                            .FontSize(7);

                        col.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                        // HEADER TABLE
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(4).Text("Description").FontSize(7).Bold();
                            row.RelativeItem(1).AlignCenter().Text("Qté").FontSize(7).Bold();
                            row.RelativeItem(2).AlignRight().Text("P.u.").FontSize(7).Bold();
                            row.RelativeItem(2).AlignRight().Text("Montant").FontSize(7).Bold();
                        });

                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                        // LIGNES
                        foreach (var line in ticket.Lines)
                        {
                            col.Item().PaddingVertical(1).Row(row =>
                            {
                                row.RelativeItem(4).Text(line.ProductName.ToUpper())
                                    .FontSize(7).ClampLines(2);
                                row.RelativeItem(1).AlignCenter()
                                    .Text(line.Quantity.ToString()).FontSize(7);
                                row.RelativeItem(2).AlignRight()
                                    .Text(line.UnitPrice.ToString("F2")).FontSize(7);
                                row.RelativeItem(2).AlignRight()
                                    .Text(line.Total.ToString("F2")).FontSize(7);
                            });
                        }

                        col.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                        // TOTALS
                        col.Item().Row(row =>
                        {
                            row.RelativeItem()
                                .Text($"TOTAL  ({ticket.Lines.Count} article{(ticket.Lines.Count > 1 ? "s" : "")})")
                                .FontSize(8);
                            row.RelativeItem().AlignRight()
                                .Text($"{ticket.Total:F2} €").FontSize(8).Bold();
                        });

                        col.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Dont TVA")
                                .FontSize(7).FontColor(Colors.Grey.Darken1);
                            row.RelativeItem().AlignRight()
                                .Text($"{ticket.VatAmount:F2} €")
                                .FontSize(7).FontColor(Colors.Grey.Darken1);
                        });

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Sous-total HT")
                                .FontSize(7).FontColor(Colors.Grey.Darken1);
                            row.RelativeItem().AlignRight()
                                .Text($"{ticket.Subtotal:F2} €")
                                .FontSize(7).FontColor(Colors.Grey.Darken1);
                        });

                        col.Item().PaddingVertical(3).LineHorizontal(1f).LineColor(Colors.Black);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL À PAYER").FontSize(10).Bold();
                            row.RelativeItem().AlignRight()
                                .Text($"{ticket.Total:F2} €").FontSize(10).Bold();
                        });

                        col.Item().PaddingVertical(3).LineHorizontal(1f).LineColor(Colors.Black);

                        // ================= PAYMENT LOGIC FIX =================

                        var totalPaidByCashOrCard = Math.Round(ticket.Payments
                            .Where(p => !string.Equals(p.Method, "Credit", StringComparison.OrdinalIgnoreCase))
                            .Sum(p => p.Amount), 2);

                        var creditTotal = Math.Round(ticket.Payments
                            .Where(p => string.Equals(p.Method, "Credit", StringComparison.OrdinalIgnoreCase))
                            .Sum(p => p.Amount), 2);

                        var remaining = Math.Round(ticket.Total - totalPaidByCashOrCard, 2);

                        bool isFullyPaid = remaining <= 0.01m;
                        bool isFullCredit = totalPaidByCashOrCard == 0 && remaining > 0.01m;
                        bool isMixedPayment = totalPaidByCashOrCard > 0 && remaining > 0.01m;

                        if (ticket.Payments.Count > 0)
                        {
                            foreach (var payment in ticket.Payments)
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text(payment.Label).FontSize(8);
                                    row.RelativeItem().AlignRight()
                                        .Text($"{payment.Amount:F2} €").FontSize(8);
                                });
                            }

                            col.Item().PaddingVertical(2)
                                .LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                            if (isFullCredit)
                            {
                                col.Item().AlignCenter()
                                    .Text("VENTE À CRÉDIT")
                                    .FontSize(10).Bold().FontColor("#C0392B");

                                col.Item().AlignCenter()
                                    .Text($"Dette client: {remaining:F2} €")
                                    .FontSize(8).FontColor("#C0392B");
                            }
                            else if (isMixedPayment)
                            {
                                col.Item().AlignCenter()
                                    .Text("PAIEMENT PARTIEL")
                                    .FontSize(9).Bold().FontColor("#D35400");

                                col.Item().AlignCenter()
                                    .Text($"Reste en crédit: {remaining:F2} €")
                                    .FontSize(8).FontColor("#D35400");
                            }
                            else if (isFullyPaid)
                            {
                                col.Item().AlignCenter()
                                    .Text("PAYÉ")
                                    .FontSize(9).Bold().FontColor("#27AE60");
                            }
                        }
                        else
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Espèces").FontSize(8);
                                row.RelativeItem().AlignRight()
                                    .Text($"{ticket.Paid:F2} €").FontSize(8);
                            });
                        }

                        if (ticket.Change > 0)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Monnaie rendue").FontSize(8);
                                row.RelativeItem().AlignRight()
                                    .Text($"{ticket.Change:F2} €").FontSize(8).Bold();
                            });
                        }

                        col.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                        // BARCODE
                        col.Item().AlignCenter().Height(45)
                            .Image(barcodeImage, ImageScaling.FitArea);

                        col.Item().AlignCenter().Text(ticket.InvoiceNumber)
                            .FontSize(7).FontColor(Colors.Black);

                        col.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                        // FOOTER
                        if (!string.IsNullOrWhiteSpace(ticket.ReceiptFooter))
                        {
                            col.Item().AlignCenter().Text(ticket.ReceiptFooter)
                                .FontSize(7).FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            col.Item().AlignCenter().Text("Merci pour votre achat !")
                                .FontSize(8).Bold();

                            col.Item().AlignCenter().Text("À bientôt dans votre magasin")
                                .FontSize(7).FontColor(Colors.Grey.Darken1);
                        }

                        col.Item().PaddingTop(2).AlignCenter()
                            .Text(ticket.SaleDate.ToString("dd/MM/yyyy HH:mm"))
                            .FontSize(6).FontColor(Colors.Grey.Lighten1);
                    });
                });
            }).GeneratePdf();
        }

        private byte[] GenerateBarcodeImage(string content)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = 80,
                    Width = 400,
                    Margin = 2,
                    PureBarcode = true
                }
            };

            var pixelData = writer.Write(content);

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