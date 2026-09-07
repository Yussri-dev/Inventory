namespace Inventory.Ui.Services.Printing
{
    public interface IPrinterDiscoveryService
    {
        Task<IReadOnlyList<PrinterInfo>>
            GetInstalledPrintersAsync(
                CancellationToken cancellationToken = default);
    }
}
