using Inventory.Dto.CashSessions.Results;
using Inventory.Services.Features.CashSession.GetById;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.CashSession.GetById
{
    public class GetCashSessionByIdQuery : IRequest<CashSessionResult>
    {
        public Guid Id { get; }

        public GetCashSessionByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
