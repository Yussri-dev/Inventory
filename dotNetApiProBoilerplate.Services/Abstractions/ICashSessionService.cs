namespace Inventory.Services.Abstractions
{
    public interface ICashSessionService
    {
        Task<Guid> EnsureActiveSessionAsync();
    }
}
