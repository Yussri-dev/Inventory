using Inventory.Dto.Customers.Results;
using MediatR;

namespace Inventory.Services.Features.Customers.GetAll
{
    public class GetAllCustomersQuery : IRequest<List<CustomerResult>>
    {
    }
}
