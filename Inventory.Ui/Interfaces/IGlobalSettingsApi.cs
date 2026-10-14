using Inventory.Dto.GlobalRequests.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IGlobalSettingsApi
    {
        [Get("/api/global-settings")]
        Task<GlobalConfigurationResult> GetAsync(
            CancellationToken cancellationToken = default);

        [Put("/api/global-settings")]
        Task<GlobalConfigurationResult> UpdateAsync(
            [Body] UpdateGlobalConfigurationRequest request,
            CancellationToken cancellationToken = default);
    }
}
