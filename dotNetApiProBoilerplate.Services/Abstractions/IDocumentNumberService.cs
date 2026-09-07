
namespace Inventory.Services.Abstractions
{
    public interface IDocumentNumberService
    {
        Task<string> GenerateAsync(string documentType);
        Task<IReadOnlyList<string>> GenerateBatchTrackedAsync(string documentType, int count);
    }
}
