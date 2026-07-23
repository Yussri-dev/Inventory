
namespace Inventory.LocalDB.Services.Interfaces
{
    public interface IReceiptPrinterTransport
    {
        Task SendAsync(
            string printerName,
            byte[] data,
            CancellationToken cancellationToken = default);
    }
}
