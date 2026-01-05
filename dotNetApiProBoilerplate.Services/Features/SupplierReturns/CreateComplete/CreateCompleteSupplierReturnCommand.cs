using Inventory.Dto.SupplierReturns.Requests;
using Inventory.Dto.SupplierReturns.Results;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.CreateComplete
{
    public class CreateCompleteSupplierReturnCommand : IRequest<SupplierReturnResult>
    {
        public CreateCompleteSupplierReturnRequest Request { get; }

        public CreateCompleteSupplierReturnCommand(CreateCompleteSupplierReturnRequest request)
        {
            Request = request;
        }
    }
}
