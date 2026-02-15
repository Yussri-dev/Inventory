
using Inventory.Dto.Sales.Results;

namespace Inventory.Services.Abstractions
{
    public interface ITicketFormatter
    {
        byte[] Format(SaleTicketResult ticket);
    }
}
