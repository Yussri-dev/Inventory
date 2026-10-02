using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Services.Printing;
using System.Drawing;
using System.Drawing.Printing;
using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace Inventory.Ui.Platforms.Windows.Printing;

// Thermal devices use ESC/POS; office printers use their Windows driver.
public sealed class WindowsReceiptPrinter(IReceiptPrinterResolver resolver,
    IReceiptPdfGenerator pdf, ReceiptPrinter thermal) : IReceiptPrinter
{
    public string? DeviceName => resolver.SelectedPrinterName;

    public async Task PrintAsync(ReceiptPrintDocument document, CancellationToken cancellationToken = default)
    {
        var name = await resolver.ResolvePrinterNameAsync(cancellationToken)
            ?? throw new InvalidOperationException("Select an available printer in Printer settings.");
        if (ReceiptPrinterResolver.IsThermalPrinter(name))
        {
            await thermal.PrintAsync(document, cancellationToken);
            return;
        }

        var bytes = await pdf.GenerateAsync(document, cancellationToken);
        using var input = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(input.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(bytes);
            await writer.StoreAsync();
            await writer.FlushAsync();
        }
        var receipt = await PdfDocument.LoadFromStreamAsync(input);
        var images = new List<Bitmap>();
        try
        {
            for (uint i = 0; i < receipt.PageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var page = receipt.GetPage(i);
                using var output = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(output, new PdfPageRenderOptions { DestinationWidth = 960 });
                using var reader = new DataReader(output.GetInputStreamAt(0));
                await reader.LoadAsync(checked((uint)output.Size));
                var png = new byte[checked((int)output.Size)];
                reader.ReadBytes(png);
                using var memory = new MemoryStream(png);
                using var image = new Bitmap(memory);
                images.Add(new Bitmap(image));
            }
            await Task.Run(() => PrintImages(name, document.Snapshot.InvoiceNumber, images, cancellationToken), cancellationToken);
        }
        finally { foreach (var image in images) image.Dispose(); }
    }

    private static void PrintImages(string printer, string invoice, List<Bitmap> images, CancellationToken ct)
    {
        using var job = new PrintDocument();
        job.PrinterSettings.PrinterName = printer;
        if (!job.PrinterSettings.IsValid) throw new InvalidOperationException($"Printer '{printer}' is unavailable.");
        job.DocumentName = $"Receipt {invoice}";
        job.PrintController = new StandardPrintController();
        job.DefaultPageSettings.Margins = new Margins(25, 25, 25, 25);
        int index = 0;
        float offset = 0;
        job.PrintPage += (_, args) =>
        {
            ct.ThrowIfCancellationRequested();
            if (index >= images.Count || args.Graphics == null) { args.HasMorePages = false; return; }
            var image = images[index];
            // Preserve the 80mm receipt width and paginate long receipts instead of shrinking them.
            var printable = RectangleF.Intersect(args.MarginBounds, args.PageSettings.PrintableArea);
            var width = Math.Min(80f / 25.4f * 100f, printable.Width);
            if (width <= 0 || printable.Height <= 0) throw new InvalidOperationException("The printer has no printable area.");
            var scale = width / image.Width;
            var height = Math.Min(image.Height - offset, printable.Height / scale);
            args.Graphics.DrawImage(image,
                new RectangleF(printable.Left - args.PageSettings.HardMarginX, printable.Top - args.PageSettings.HardMarginY, width, height * scale),
                new RectangleF(0, offset, image.Width, height), GraphicsUnit.Pixel);
            offset += height;
            if (offset >= image.Height - 0.01f) { index++; offset = 0; }
            args.HasMorePages = index < images.Count;
        };
        ct.ThrowIfCancellationRequested();
        job.Print();
    }
}
