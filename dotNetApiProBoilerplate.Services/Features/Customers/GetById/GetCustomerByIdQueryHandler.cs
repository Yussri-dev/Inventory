using Inventory.Dto.Customers.Results;
using MediatR;

namespace Inventory.Services.Features.Customers.GetById
{
    
    public class GetCustomerByIdQueryHandler
        : IRequestHandler<GetCustomerByIdQuery, CustomerResult>
    {
        private readonly CustomerService _customerService;

        public GetCustomerByIdQueryHandler(CustomerService customerService)
        {
            _customerService = customerService;
        }

        public Task<CustomerResult> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
