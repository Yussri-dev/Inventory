using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Customers.Results;
using MediatR;

namespace Inventory.Services.Features.Customers.Update
{
    
    public class UpdateCustomerCommand : IRequest<CustomerResult>
    {
        public Guid Id { get; }
        public UpdateCustomerRequest Request { get; }

        public UpdateCustomerCommand(Guid id, UpdateCustomerRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
