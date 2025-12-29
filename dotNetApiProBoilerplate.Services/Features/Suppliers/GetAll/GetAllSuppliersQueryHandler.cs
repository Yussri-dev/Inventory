using Inventory.Dto.Suppliers.Results;
using MediatR;

namespace Inventory.Services.Features.Suppliers.GetAll
{
    public class GetAllSuppliersQueryHandler
        : IRequestHandler<GetAllSuppliersQuery, List<SupplierResult>>
    {
        private readonly SupplierService _customerService;

        public GetAllSuppliersQueryHandler(SupplierService customerService)
        {
            _customerService = customerService;
        }

        public Task<List<SupplierResult>> Handle(GetAllSuppliersQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetAllAsync();
        }
    }
}
