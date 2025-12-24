using Inventory.Dto.Customers.Results;
using MediatR;

namespace Inventory.Services.Features.Customers.Update
{
    
    public class UpdateCustomerCommandHandler
        : IRequestHandler<UpdateCustomerCommand, CustomerResult>
    {
        private readonly CustomerService _customerService;

        public UpdateCustomerCommandHandler(CustomerService customerService)
        {
            _customerService = customerService;
        }

        public Task<CustomerResult> Handle(UpdateCustomerCommand command, CancellationToken cancellationToken)
        {
            return _customerService.UpdateAsync(command.Id, command.Request);
        }
    }
}
