using Inventory.Dto.Suppliers.Results;
using MediatR;

namespace Inventory.Services.Features.Suppliers.GetById
{
    public class GetSupplierByIdQueryHandler
        : IRequestHandler<GetSupplierByIdQuery, SupplierResult>
    {
        private readonly SupplierService _customerService;

        public GetSupplierByIdQueryHandler(SupplierService customerService)
        {
            _customerService = customerService;
        }

        public Task<SupplierResult> Handle(GetSupplierByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
