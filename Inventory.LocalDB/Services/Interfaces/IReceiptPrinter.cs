

using Inventory.LocalDB.Models;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface IReceiptPrinter
    {
        string? DeviceName { get; }

        Task PrintAsync(
            ReceiptPrintDocument document,
            CancellationToken cancellationToken = default);
    }
}
