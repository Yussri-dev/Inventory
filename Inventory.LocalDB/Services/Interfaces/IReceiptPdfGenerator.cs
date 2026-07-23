

using Inventory.LocalDB.Models;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface IReceiptPdfGenerator
    {
        Task<byte[]> GenerateAsync(
            ReceiptPrintDocument document,
            CancellationToken cancellationToken = default);
    }
}
