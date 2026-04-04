using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.Pending
{
    public class PendingSaleCommand : IRequest<SaleResult>
    {
        public CreatePendingSaleRequest Request { get; }

        public PendingSaleCommand(CreatePendingSaleRequest request)
        {
            Request = request;
        }
    }
}
