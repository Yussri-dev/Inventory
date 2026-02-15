using Inventory.Dto.Sales.Results;

namespace Inventory.Ui.Interfaces
{
    public interface ITicketPrinter
    {
        Task PrintAsync(Guid saleId);
    }
}
