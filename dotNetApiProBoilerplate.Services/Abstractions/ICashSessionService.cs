using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;

namespace Inventory.Services.Abstractions;

public interface ICashSessionService
{
    Task<CashSessionResult?> GetActiveAsync();

    Task<Guid> EnsureActiveSessionAsync();

    Task<CashSessionResult> CreateAsync(
        CreateCashSessionRequest request);

    Task<CashSessionResult> CloseSessionAsync(
        Guid id,
        CloseCashSessionRequest request);

    Task<CashSessionResult> GetByIdAsync(
        Guid id);

    Task<List<CashSessionResult>> GetAllAsync();

    Task<CashSessionResult> UpdateAsync(
        Guid id,
        UpdateCashSessionRequest request);

    Task<bool> DeleteAsync(
        Guid id);

    Task<PagedResult<CashSessionResult>> QueryAsync(
        CashSessionQuery query);
}