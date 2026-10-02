using Inventory.LocalDB.Services.Interfaces;

namespace Inventory.Ui.Services.Printing
{
    public sealed class ReceiptPrinterResolver
        : IReceiptPrinterResolver
    {
        private static readonly string[]
            VirtualPrinterMarkers =
            {
                "PDF",
                "XPS",
                "ONENOTE",
                "FAX",
                "DOCUMENT WRITER",
                "PRINT TO FILE",
                "MICROSOFT PRINT",
                "ADOBE PDF",
                "SEND TO"
            };

        private static readonly string[]
            ThermalPrinterMarkers =
            {
                "RECEIPT",
                "THERMAL",
                "POS",
                "80MM",
                "58MM",
                "EPSON TM",
                "BIXOLON",
                "XPRINTER",
                "X PRINTER",
                "GP-",
                "GPRINTER",
                "HPRT",
                "RONGTA",
                "ZJIANG",
                "SEWOO",
                "SNBC",
                "STAR TSP",
                "STAR MCP",
                "CITIZEN CT",
                "CITIZEN CBM"
            };

        private readonly IPrinterDiscoveryService
            _printerDiscoveryService;

        private readonly IReceiptPrinterSettingsService
            _printerSettingsService;

        private readonly SemaphoreSlim _resolutionLock =
            new(1, 1);

        public ReceiptPrinterResolver(
            IPrinterDiscoveryService printerDiscoveryService,
            IReceiptPrinterSettingsService printerSettingsService)
        {
            _printerDiscoveryService =
                printerDiscoveryService;

            _printerSettingsService =
                printerSettingsService;
        }

        public string? SelectedPrinterName => _printerSettingsService
        .GetSelectedPrinterName();

        public async Task<string?>
            ResolvePrinterNameAsync(
                CancellationToken cancellationToken = default)
        {
            await _resolutionLock.WaitAsync(
                cancellationToken);

            try
            {
                var installedPrinters =
                    await _printerDiscoveryService
                        .GetInstalledPrintersAsync(
                            cancellationToken);

                var selectedPrinterName =
                    _printerSettingsService
                        .GetSelectedPrinterName();

                if (!string.IsNullOrWhiteSpace(
                        selectedPrinterName))
                {
                    var selectedPrinter =
                        installedPrinters
                            .FirstOrDefault(printer =>
                                string.Equals(
                                    printer.Name,
                                    selectedPrinterName,
                                    StringComparison.OrdinalIgnoreCase));

                    if (selectedPrinter != null && !IsVirtualPrinter(selectedPrinter.Name))
                    {
                        return selectedPrinter.Name;
                    }

                    /*
                     * L’imprimante enregistrée a été supprimée,
                     * renommée ou son pilote n’est plus installé.
                     */
                    _printerSettingsService
                        .ClearSelectedPrinter();
                }

                // Respect the Windows default even when it is an ordinary office printer.
                var defaultPrinter = installedPrinters.FirstOrDefault(p => p.IsDefault && !IsVirtualPrinter(p.Name));
                if (defaultPrinter != null) return defaultPrinter.Name;

                var thermalCandidates =
                    installedPrinters
                        .Where(printer =>
                            !IsVirtualPrinter(
                                printer.Name) &&
                            IsThermalPrinter(
                                printer.Name))
                        .ToList();

                /*
                 * Sélection automatique lorsqu’une seule
                 * imprimante thermique est détectée.
                 */
                if (thermalCandidates.Count == 1)
                {
                    return SelectAndReturn(
                        thermalCandidates[0].Name);
                }

                /*
                 * Plusieurs imprimantes thermiques :
                 * utiliser celle définie comme imprimante
                 * Windows par défaut si elle est unique.
                 */
                var defaultThermalPrinters =
                    thermalCandidates
                        .Where(printer =>
                            printer.IsDefault)
                        .ToList();

                if (defaultThermalPrinters.Count == 1)
                {
                    return SelectAndReturn(
                        defaultThermalPrinters[0].Name);
                }

                /*
                 * Aucun choix sûr.
                 * L’écran de configuration permettra
                 * une sélection manuelle.
                 */
                return null;
            }
            finally
            {
                _resolutionLock.Release();
            }
        }

        public async Task<bool>
            IsPrinterAvailableAsync(
                string printerName,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                return false;
            }

            var installedPrinters =
                await _printerDiscoveryService
                    .GetInstalledPrintersAsync(
                        cancellationToken);

            return installedPrinters.Any(printer =>
                string.Equals(
                    printer.Name,
                    printerName,
                    StringComparison.OrdinalIgnoreCase));
        }

        private string SelectAndReturn(
            string printerName)
        {
            _printerSettingsService.SelectPrinter(
                printerName);

            return printerName;
        }

        private static bool IsVirtualPrinter(
            string printerName)
        {
            return ContainsAnyMarker(
                printerName,
                VirtualPrinterMarkers);
        }

        public static bool IsThermalPrinter(
            string printerName)
        {
            return ContainsAnyMarker(
                printerName,
                ThermalPrinterMarkers);
        }

        private static bool ContainsAnyMarker(
            string printerName,
            IEnumerable<string> markers)
        {
            return markers.Any(marker =>
                printerName.Contains(
                    marker,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
