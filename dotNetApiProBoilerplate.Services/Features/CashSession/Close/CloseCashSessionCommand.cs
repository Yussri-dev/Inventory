using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.CashSession.Close
{
    public class CloseCashSessionCommand : IRequest<CashSessionResult>
    {
        public Guid Id { get; }
        public CloseCashSessionRequest Request { get; }

        public CloseCashSessionCommand(Guid id, CloseCashSessionRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
