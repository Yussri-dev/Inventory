using Inventory.Dto.GlobalRequests.Results;

namespace Inventory.Services.Abstractions
{
    public interface IGlobalSettingService
    {
        Task<GlobalConfigurationResult> GetAsync(
            CancellationToken cancellationToken = default);

        Task<GlobalConfigurationResult> UpdateAsync(
            UpdateGlobalConfigurationRequest request,
            Guid? updatedByUserId,
            CancellationToken cancellationToken = default);
    }
}
