using Inventory.Dto.Customers.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Customers.GetAll
{
    public class GetAllCustomersQuery : IRequest<List<CustomerResult>>
    {
    }
}
