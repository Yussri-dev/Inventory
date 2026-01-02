using Inventory.Dto.LoyaltyCards.Results;
using Inventory.Dto.LoyaltyCards.Requests;
using Inventory.Services.Features.LoyaltyCards.Create;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.LoyaltyCards.Create
{
    public class CreateLoyaltyCardCommand : IRequest<LoyaltyCardResult>
    {
        public CreateLoyaltyCardRequest Request { get; }

        public CreateLoyaltyCardCommand(CreateLoyaltyCardRequest request)
        {
            Request = request;
        }
    }
}
