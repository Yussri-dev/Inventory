using Inventory.Dto.Purchases.Results;
using Inventory.Services.Features.Purchases.GetAll;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Purchases.GetAll
{
    public class GetAllPurchasesQuery : IRequest<List<PurchaseResult>>
    {
    }
}
