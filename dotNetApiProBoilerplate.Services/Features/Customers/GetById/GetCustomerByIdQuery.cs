using Inventory.Dto.Customers.Results;
using MediatR;

namespace Inventory.Services.Features.Customers.GetById
{
    public class GetCustomerByIdQuery : IRequest<CustomerResult>
    {
        public Guid Id { get; }

        public GetCustomerByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
