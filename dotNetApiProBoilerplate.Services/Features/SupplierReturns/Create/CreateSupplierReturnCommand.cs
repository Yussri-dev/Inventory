using Inventory.Dto.SupplierReturns.Results;
using Inventory.Dto.SupplierReturns.Requests;
using Inventory.Services.Features.SupplierReturns.Create;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.Create
{
    public class CreateSupplierReturnCommand : IRequest<SupplierReturnResult>
    {
        public CreateSupplierReturnRequest Request { get; }

        public CreateSupplierReturnCommand(CreateSupplierReturnRequest request)
        {
            Request = request;
        }
    }

}
