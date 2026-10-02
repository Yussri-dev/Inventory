using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inventory.Dto.AppUpdates;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services.Updates;

public sealed class AppUpdateService(HttpClient client, ILogger<AppUpdateService> logger) : IAppUpdateService
{
    private string CacheKey => "windows-update-policy-" + client.BaseAddress?.Host + "-" + client.BaseAddress?.Port;
    public string InstalledVersion => AppInfo.Current.VersionString;

    public async Task<AppUpdateManifest?> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            using var response = await client.GetAsync("/api/app-updates/windows", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                Preferences.Default.Remove(CacheKey);
                return null;
            }
            response.EnsureSuccessStatusCode();
            var manifest = await response.Content.ReadFromJsonAsync<AppUpdateManifest>(cancellationToken);
            if (manifest == null || !manifest.IsValid()) throw new InvalidOperationException("Invalid update metadata.");
            Preferences.Default.Set(CacheKey, JsonSerializer.Serialize(manifest));
            return manifest;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error) { logger.LogWarning(error, "Update check unavailable; using the last valid local policy."); }

        try
        {
            var json = Preferences.Default.Get(CacheKey, "");
            var cached = string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<AppUpdateManifest>(json);
            return cached?.IsValid() == true ? cached : null;
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Cannot read cached update policy.");
            return null;
        }
    }

    public async Task OpenDownloadAsync(AppUpdateManifest manifest)
    {
        if (!manifest.IsValid()) throw new InvalidOperationException("The download link is invalid.");
        if (!await Launcher.Default.OpenAsync(new Uri(manifest.DownloadUrl)))
            throw new InvalidOperationException("Unable to open the installer download page.");
    }
}
