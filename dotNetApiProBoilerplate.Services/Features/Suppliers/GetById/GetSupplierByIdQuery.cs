using Inventory.Dto.Suppliers.Results;
using MediatR;

namespace Inventory.Services.Features.Suppliers.GetById
{
    public class GetSupplierByIdQuery : IRequest<SupplierResult>
    {
        public Guid Id { get; }

        public GetSupplierByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
