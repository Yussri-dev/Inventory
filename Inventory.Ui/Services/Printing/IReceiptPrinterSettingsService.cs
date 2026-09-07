namespace Inventory.Ui.Services.Printing
{
    public interface IReceiptPrinterSettingsService
    {
        string? GetSelectedPrinterName();

        void SelectPrinter(string printerName);

        void ClearSelectedPrinter();
    }
}
