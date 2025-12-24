using Inventory.Dto.Customers.Results;
using Inventory.Services.Features.Products.Create;
using MediatR;

namespace Inventory.Services.Features.Customers.Create
{
    public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerResult>
    {
        private readonly CustomerService _customerService;

        public CreateCustomerCommandHandler(CustomerService productService)
        {
            _customerService = productService;
        }

        public Task<CustomerResult> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }
}
