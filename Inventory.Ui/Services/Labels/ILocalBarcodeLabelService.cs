
namespace Inventory.Ui.Services.Labels
{
    public interface ILocalBarcodeLabelService
    {
        Task<byte[]> GenerateAsync(
            Guid localProductId,
            CancellationToken cancellationToken = default);
    }
}
