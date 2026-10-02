using Inventory.Dto.AppUpdates;

namespace Inventory.Ui.Services.Updates;

public interface IAppUpdateService
{
    string InstalledVersion { get; }
    Task<AppUpdateManifest?> CheckAsync(CancellationToken cancellationToken = default);
    Task OpenDownloadAsync(AppUpdateManifest manifest);
}
