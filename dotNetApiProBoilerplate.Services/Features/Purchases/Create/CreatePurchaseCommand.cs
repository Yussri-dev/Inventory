using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Purchases.Requests;
using Inventory.Services.Features.Purchases.Create;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Purchases.Create
{
    public class CreatePurchaseCommand : IRequest<PurchaseResult>
    {
        public CreatePurchaseRequest Request { get; }

        public CreatePurchaseCommand(CreatePurchaseRequest request)
        {
            Request = request;
        }
    }
}
