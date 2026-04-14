using Inventory.Domain.Entities;
using Inventory.Dto.CashSessions.Results;

namespace Inventory.Services.Abstractions
{
    public interface ICashSessionService
    {
        Task<Guid> EnsureActiveSessionAsync();
        Task<CashSessionResult?> GetActiveAsync();
    }
}
