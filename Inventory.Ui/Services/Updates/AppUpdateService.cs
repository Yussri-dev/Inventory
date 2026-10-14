using System.Xml.Linq;
using Inventory.Dto.AppUpdates;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services.Updates;

public sealed class AppUpdateService(HttpClient client, ILogger<AppUpdateService> logger)
    : IAppUpdateService
{
    public string InstalledVersion => AppInfo.Current.VersionString;

    public async Task<AppUpdateManifest?> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            var xml = await client.GetStringAsync(
                "https://api.ventepilote.com/updates/Inventory.Ui.appinstaller",
                cancellationToken);

            var document = XDocument.Parse(xml);

            XNamespace ns = "http://schemas.microsoft.com/appx/appinstaller/2018";

            var mainPackage = document
                .Root?
                .Element(ns + "MainPackage");

            if (mainPackage == null)
                return null;

            var version = mainPackage.Attribute("Version")?.Value;
            var uri = mainPackage.Attribute("Uri")?.Value;

            if (string.IsNullOrWhiteSpace(version) ||
                string.IsNullOrWhiteSpace(uri))
            {
                return null;
            }

            return new AppUpdateManifest
            {
                LatestVersion = version,
                MinimumVersion = "1.0.0.1",
                DownloadUrl = "https://api.ventepilote.com/updates/Inventory.Ui.appinstaller",
                ReleaseNotes = null
            };
        }
        catch (Exception error)
        {
            logger.LogWarning(
                error,
                "Unable to check Windows App Installer manifest.");

            return null;
        }
    }

    public async Task OpenDownloadAsync(AppUpdateManifest manifest)
    {
        if (!manifest.IsValid())
            throw new InvalidOperationException(
                "The download link is invalid.");

        if (!await Launcher.Default.OpenAsync(
                new Uri(manifest.DownloadUrl)))
        {
            throw new InvalidOperationException(
                "Unable to open the installer download page.");
        }
    }
}