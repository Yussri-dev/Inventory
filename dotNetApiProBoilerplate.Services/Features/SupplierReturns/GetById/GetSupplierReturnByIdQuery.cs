using Inventory.Dto.SupplierReturns.Results;
using Inventory.Services.Features.SupplierReturns.GetById;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.GetById
{
    public class GetSupplierReturnByIdQuery : IRequest<SupplierReturnResult>
    {
        public Guid Id { get; }

        public GetSupplierReturnByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
