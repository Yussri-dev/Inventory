namespace Inventory.Dto.AppUpdates;

public sealed class AppUpdateManifest
{
    public string LatestVersion { get; set; } = "";
    public string MinimumVersion { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string? ReleaseNotes { get; set; }

    public bool IsValid()
    {
        return
            TryVersion(LatestVersion, out var latest) &&
            TryVersion(MinimumVersion, out var minimum) &&
            minimum <= latest &&
            Uri.TryCreate(
                DownloadUrl,
                UriKind.Absolute,
                out var url) &&
            url.Scheme == Uri.UriSchemeHttps &&
            !string.IsNullOrWhiteSpace(url.Host) &&
            string.IsNullOrEmpty(url.UserInfo);
    }

    public AppUpdateStatus Compare(string installed)
    {
        if (!IsValid() || !TryVersion(installed, out var current)) return AppUpdateStatus.None;
        TryVersion(MinimumVersion, out var minimum);
        TryVersion(LatestVersion, out var latest);
        return current < minimum ? AppUpdateStatus.Required : current < latest ? AppUpdateStatus.Available : AppUpdateStatus.None;
    }

    private static bool TryVersion(
     string text,
     out Version version)
    {
        version = new Version(0, 0, 0);

        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (!Version.TryParse(text.Trim(), out var parsed))
            return false;

        if (parsed.Build < 0)
            return false;

        version = new Version(
            parsed.Major,
            parsed.Minor,
            parsed.Build,
            Math.Max(0, parsed.Revision));

        return true;
    }
}

public enum AppUpdateStatus { None, Available, Required }
