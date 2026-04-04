using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Sales.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Sales.GetByCustomer
{
    public class GetSalesByCustomerQuery : IRequest<PagedResult<SaleResult>>
    {
        public CustomerSaleQuery Query { get; }

        public GetSalesByCustomerQuery(CustomerSaleQuery query)
        {
            Query = query;
        }
    }
}
