using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Customers.Results;
using MediatR;

namespace Inventory.Services.Features.Customers.Create
{
    public class CreateCustomerCommand : IRequest<CustomerResult>
    {
        public CreateCustomerRequest Request { get; }

        public CreateCustomerCommand(CreateCustomerRequest request)
        {
            Request = request;
        }
    }
}
