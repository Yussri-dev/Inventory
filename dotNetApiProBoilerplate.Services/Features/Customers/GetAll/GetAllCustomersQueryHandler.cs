using Inventory.Dto.Customers.Results;
using MediatR;

namespace Inventory.Services.Features.Customers.GetAll
{
    public class GetAllCustomersQueryHandler
        : IRequestHandler<GetAllCustomersQuery, List<CustomerResult>>
    {
        private readonly CustomerService _customerService;

        public GetAllCustomersQueryHandler(CustomerService customerService)
        {
            _customerService = customerService;
        }

        public Task<List<CustomerResult>> Handle(GetAllCustomersQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetAllAsync();
        }
    }
}
