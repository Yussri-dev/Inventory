namespace Inventory.LocalDB.Services.Interfaces
{
    public interface IReceiptPrinterResolver
    {
        string? SelectedPrinterName { get; }

        Task<string?> ResolvePrinterNameAsync(
            CancellationToken cancellationToken = default);

        Task<bool> IsPrinterAvailableAsync(
            string printerName,
            CancellationToken cancellationToken = default);
    }
}
