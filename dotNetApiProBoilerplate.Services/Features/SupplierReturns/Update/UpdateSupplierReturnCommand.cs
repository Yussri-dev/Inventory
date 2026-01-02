using Inventory.Dto.SupplierReturns.Results;
using Inventory.Dto.SupplierReturns.Requests;
using Inventory.Services.Features.SupplierReturns.Update;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.Update
{
    public class UpdateSupplierReturnCommand : IRequest<SupplierReturnResult>
    {
        public Guid Id { get; }
        public UpdateSupplierReturnRequest Request { get; }

        public UpdateSupplierReturnCommand(Guid id, UpdateSupplierReturnRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
