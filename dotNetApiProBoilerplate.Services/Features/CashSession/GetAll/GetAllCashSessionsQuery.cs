using Inventory.Dto.CashSessions.Results;
using MediatR;

namespace Inventory.Services.Features.CashSession.GetAll
{
    public class GetAllCashSessionsQuery : IRequest<List<CashSessionResult>>
    {
    }
}
