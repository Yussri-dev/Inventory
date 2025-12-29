using Inventory.Dto.Suppliers.Results;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Services.Features.Suppliers.Update;
using MediatR;

namespace Inventory.Services.Features.Suppliers.Update
{
    public class UpdateSupplierCommand : IRequest<SupplierResult>
    {
        public Guid Id { get; }
        public UpdateSupplierRequest Request { get; }

        public UpdateSupplierCommand(Guid id, UpdateSupplierRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
