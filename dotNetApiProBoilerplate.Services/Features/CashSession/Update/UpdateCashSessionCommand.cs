using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.CashSessions.Requests;
using Inventory.Services.Features.CashSession.Update;
using MediatR;

namespace Inventory.Services.Features.CashSession.Update
{
    public class UpdateCashSessionCommand : IRequest<CashSessionResult>
    {
        public Guid Id { get; }
        public UpdateCashSessionRequest Request { get; }

        public UpdateCashSessionCommand(Guid id, UpdateCashSessionRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
