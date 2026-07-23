
namespace Inventory.LocalDB.Services.Results
{
    public class LocalReceiptPrintResult
    {
        public bool Success { get; init; }
        public Guid? PrintLogId { get; init; }
        public bool IsDuplicate { get; init; }

        public int CopyNumber { get; init; }

        public string? ErrorMessage { get; init; }

        public static LocalReceiptPrintResult Succeeded(Guid printLogId, bool IsDuplicate, int copyNumber)
        {
            return new LocalReceiptPrintResult
            {
                Success = true,
                PrintLogId = printLogId,
                IsDuplicate = IsDuplicate,
                CopyNumber = copyNumber,
                ErrorMessage = null
            };
        }

        public static LocalReceiptPrintResult Failed(string errorMessage, bool isDuplicate = false, int copyNumber = 0, Guid? printLogId = null)
        {
            return new LocalReceiptPrintResult
            {
                Success = false,
                PrintLogId = printLogId,
                IsDuplicate = isDuplicate,
                CopyNumber = copyNumber,
                ErrorMessage = errorMessage
            };
        }
    }
}
