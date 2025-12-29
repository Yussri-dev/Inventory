using Inventory.Dto.Sales.Results;
using Inventory.Dto.Sales.Requests;
using Inventory.Services.Features.Sales.Update;
using MediatR;

namespace Inventory.Services.Features.Sales.Update
{
    public class UpdateSaleCommand : IRequest<SaleResult>
    {
        public Guid Id { get; }
        public UpdateSaleRequest Request { get; }

        public UpdateSaleCommand(Guid id, UpdateSaleRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
