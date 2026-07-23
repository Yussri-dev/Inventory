using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Inventory.LocalDB.Services
{
    public sealed class ReceiptPrinter : IReceiptPrinter
    {
        private readonly IReceiptPrinterTransport _transport;
        private readonly ReceiptPrinterOptions _options;
        private readonly ILogger<ReceiptPrinter> _logger;

        public ReceiptPrinter(
            IReceiptPrinterTransport transport,
            IOptions<ReceiptPrinterOptions> options,
            ILogger<ReceiptPrinter> logger)
        {
            _transport =
                transport;

            _options =
                options.Value;

            _logger =
                logger;
        }

        public string? DeviceName =>
            _options.PrinterName;

        public async Task PrintAsync(
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

            if (string.IsNullOrWhiteSpace(
                    _options.PrinterName))
            {
                throw new InvalidOperationException(
                    "No receipt printer has been configured.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var bytes =
                ReceiptEscPosBuilder.Build(
                    document,
                    _options);

            if (bytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "The generated receipt is empty.");
            }

            _logger.LogInformation(
                "Printing receipt {InvoiceNumber} on {PrinterName}. " +
                "Duplicate={Duplicate}, Copy={CopyNumber}.",
                document.Snapshot.InvoiceNumber,
                _options.PrinterName,
                document.IsDuplicate,
                document.CopyNumber);

            await _transport.SendAsync(
                _options.PrinterName,
                bytes,
                cancellationToken);

            _logger.LogInformation(
                "Receipt {InvoiceNumber} sent successfully to {PrinterName}.",
                document.Snapshot.InvoiceNumber,
                _options.PrinterName);
        }
    }
}
