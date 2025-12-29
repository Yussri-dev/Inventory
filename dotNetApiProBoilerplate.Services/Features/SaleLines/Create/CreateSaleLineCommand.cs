
using Inventory.Dto.SaleLines.Requests;
using Inventory.Dto.SaleLines.Results;
using Inventory.Dto.Sales.Results;
using Inventory.Services.Features.Sales.Create;
using MediatR;

namespace Inventory.Services.Features.SaleLines.Create
{
    public class CreateSaleLineCommand : IRequest<SaleLineResult>
    {
        public CreateSaleLineRequest Request { get; }

        public CreateSaleLineCommand(CreateSaleLineRequest request)
        {
            Request = request;
        }
    }
}
