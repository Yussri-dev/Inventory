
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Results;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface IReceiptService
    {
        Task<LocalReceipt> CreateReceiptAsync(
            Guid localSaleId,
            CancellationToken cancellationToken = default);

        Task<LocalReceiptPrintResult> PrintOriginalAsync(
            Guid receiptId,
            CancellationToken cancellationToken = default);

        Task<LocalReceiptPrintResult> PrintDuplicateAsync(
            Guid receiptId,
            string? reason = null,
            CancellationToken cancellationToken = default);

        Task<byte[]> GeneratePdfAsync(
            Guid receiptId,
            bool duplicate,
            CancellationToken cancellationToken = default);
    }
}
