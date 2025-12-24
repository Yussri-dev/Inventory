using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Customers.Delete
{
    public class DeleteCustomerCommandHandler
         : IRequestHandler<DeleteCustomerCommand, Unit>
    {
        private readonly CustomerService _customerService;

        public DeleteCustomerCommandHandler(CustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteCustomerCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
