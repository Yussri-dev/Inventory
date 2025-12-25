using Inventory.Dto.Purchases.Results;
using Inventory.Services.Features.Purchases.GetById;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Purchases.GetById
{
    public class GetPurchaseByIdQuery : IRequest<PurchaseResult>
    {
        public Guid Id { get; }

        public GetPurchaseByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
