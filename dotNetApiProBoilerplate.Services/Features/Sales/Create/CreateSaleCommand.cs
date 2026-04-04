using Inventory.Dto.Sales.Results;
using Inventory.Dto.Sales.Requests;
using MediatR;

namespace Inventory.Services.Features.Sales.Create
{
    public class CreateSaleCommand : IRequest<SaleResult>
    {
        public CreateSaleRequest Request { get; }

        public CreateSaleCommand(CreateSaleRequest request)
        {
            Request = request;
        }
    }
}
