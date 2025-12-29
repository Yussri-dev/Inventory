using Inventory.Dto.Suppliers.Results;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Services.Features.Suppliers.Create;
using MediatR;

namespace Inventory.Services.Features.Suppliers.Create
{
    public class CreateSupplierCommand : IRequest<SupplierResult>
    {
        public CreateSupplierRequest Request { get; }

        public CreateSupplierCommand(CreateSupplierRequest request)
        {
            Request = request;
        }
    }
}
