#if WINDOWS

using Inventory.Dto.Sales.Results;
using Inventory.Ui.Interfaces;
using System.Net.Http.Json;


namespace Inventory.Ui.Platforms.Windows.Printing
{
    //public class WindowsTicketPrinter : ITicketPrinter
    //{
    //    private const string PrinterName = "POS-80";
    //    private readonly ISaleApi _saleApi;

    //    public WindowsTicketPrinter(ISaleApi saleApi)
    //    {
    //        _saleApi = saleApi;
    //    }

    //    public async Task PrintAsync(Guid saleId)
    //    {
    //        var response = await _saleApi.GetTicket(saleId);

    //        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

    //        RawPrinterHelper.SendBytes(PrinterName, pdfBytes);
    //    }
    //}
}
#endif