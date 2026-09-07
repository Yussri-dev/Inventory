namespace Inventory.Ui.Services.Printing
{
    public sealed class ReceiptPrinterSettingsService
        : IReceiptPrinterSettingsService
    {
        private const string SelectedPrinterNameKey =
            "receipt-printer.selected-printer-name";

        public string? GetSelectedPrinterName()
        {
            var printerName =
                Preferences.Default.Get(
                    SelectedPrinterNameKey,
                    string.Empty);

            return string.IsNullOrWhiteSpace(printerName)
                ? null
                : printerName.Trim();
        }

        public void SelectPrinter(string printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                throw new ArgumentException(
                    "Printer name is required.",
                    nameof(printerName));
            }

            Preferences.Default.Set(
                SelectedPrinterNameKey,
                printerName.Trim());
        }

        public void ClearSelectedPrinter()
        {
            Preferences.Default.Remove(
                SelectedPrinterNameKey);
        }
    }
}
