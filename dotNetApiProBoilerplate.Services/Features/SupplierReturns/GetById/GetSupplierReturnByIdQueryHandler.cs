using Inventory.Dto.SupplierReturns.Results;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.GetById
{
    public class GetSupplierReturnByIdQueryHandler
        : IRequestHandler<GetSupplierReturnByIdQuery, SupplierReturnResult>
    {
        private readonly SupplierReturnService _customerService;

        public GetSupplierReturnByIdQueryHandler(SupplierReturnService customerService)
        {
            _customerService = customerService;
        }

        public Task<SupplierReturnResult> Handle(GetSupplierReturnByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
