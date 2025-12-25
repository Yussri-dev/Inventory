using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.CashSessions.Requests;
using MediatR;

namespace Inventory.Services.Features.CashSession.Create
{
    public class CreateCashSessionCommand : IRequest<CashSessionResult>
    {
        public CreateCashSessionRequest Request { get; }

        public CreateCashSessionCommand(CreateCashSessionRequest request)
        {
            Request = request;
        }
    }
}
