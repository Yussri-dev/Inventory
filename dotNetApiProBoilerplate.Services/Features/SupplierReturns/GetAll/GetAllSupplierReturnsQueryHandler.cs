using Inventory.Dto.SupplierReturns.Results;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.GetAll
{
    public class GetAllSupplierReturnsQueryHandler
        : IRequestHandler<GetAllSupplierReturnsQuery, List<SupplierReturnResult>>
    {
        private readonly SupplierReturnService _customerService;

        public GetAllSupplierReturnsQueryHandler(SupplierReturnService customerService)
        {
            _customerService = customerService;
        }

        public Task<List<SupplierReturnResult>> Handle(GetAllSupplierReturnsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetAllAsync();
        }
    }
}
