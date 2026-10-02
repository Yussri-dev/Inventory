using Inventory.Ui.Services.Printing;

namespace Inventory.Api.Tests;

public class ReceiptPrinterResolverTests
{
    [Fact]
    public async Task UsesOfficePrinterDefaultWithoutPersistingIt()
    {
        var settings = new Settings();
        var resolver = new ReceiptPrinterResolver(new Discovery(
            new() { Name = "HP DeskJet 4300 series", IsDefault = true },
            new() { Name = "EPSON TM-T20" }), settings);
        Assert.Equal("HP DeskJet 4300 series", await resolver.ResolvePrinterNameAsync());
        Assert.Null(settings.Name);
    }

    [Fact]
    public async Task ExplicitPhysicalSelectionTakesPrecedence()
    {
        var resolver = new ReceiptPrinterResolver(new Discovery(
            new() { Name = "HP DeskJet 4300 series", IsDefault = true },
            new() { Name = "EPSON TM-T20" }), new Settings { Name = "EPSON TM-T20" });
        Assert.Equal("EPSON TM-T20", await resolver.ResolvePrinterNameAsync());
    }

    [Fact]
    public async Task AvoidsSelectedVirtualPrinterAndUsesThermalFallback()
    {
        var settings = new Settings { Name = "Microsoft Print to PDF" };
        var resolver = new ReceiptPrinterResolver(new Discovery(
            new() { Name = "Microsoft Print to PDF", IsDefault = true },
            new() { Name = "EPSON TM-T20" }), settings);
        Assert.Equal("EPSON TM-T20", await resolver.ResolvePrinterNameAsync());
        Assert.Equal("EPSON TM-T20", settings.Name);
    }

    [Fact]
    public async Task DoesNotSelectVirtualOnlyPrinter()
    {
        var resolver = new ReceiptPrinterResolver(new Discovery(
            new PrinterInfo { Name = "Microsoft Print to PDF", IsDefault = true }), new Settings());
        Assert.Null(await resolver.ResolvePrinterNameAsync());
    }

    private sealed class Discovery(params PrinterInfo[] printers) : IPrinterDiscoveryService
    {
        public Task<IReadOnlyList<PrinterInfo>> GetInstalledPrintersAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PrinterInfo>>(printers);
    }

    private sealed class Settings : IReceiptPrinterSettingsService
    {
        public string? Name;
        public string? GetSelectedPrinterName() => Name;
        public void SelectPrinter(string printerName) => Name = printerName;
        public void ClearSelectedPrinter() => Name = null;
    }
}
