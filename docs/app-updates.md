# Windows application updates

The app checks `GET /api/app-updates/windows` at startup, using
`AppInfo.Current.VersionString`. Numeric versions are compared numerically;
`1.0` and `1.0.0` are equivalent.

- Installed version at or above LatestVersion: normal access.
- Installed version below LatestVersion but at or above MinimumVersion: dismissible notification.
- Installed version below MinimumVersion: blocking screen with download and recheck actions.

## Enable a release

Publish and verify the Windows installer first, then configure the deployed API
using these environment variables (replace the example URL with your real HTTPS URL):

```text
AppUpdates__Windows__Enabled=true
AppUpdates__Windows__LatestVersion=1.0.1
AppUpdates__Windows__MinimumVersion=1.0.0
AppUpdates__Windows__DownloadUrl=https://downloads.example.com/Inventory-1.0.1.msix
AppUpdates__Windows__ReleaseNotes=Receipt printing improvements.
```

No release is enabled by default. Missing/false Enabled returns HTTP 204.
Invalid enabled configuration returns HTTP 503. The endpoint is anonymous so
users can update before login. It contains only public release information.
Deploy the API and verify the endpoint response before distributing the client.
The client uses the API base URL configured in `Inventory.Ui/MauiProgram.cs`;
set this to your deployed API for production builds.

To require 1.0.1, set MinimumVersion to 1.0.1. Do this only once the installer is
available and tested. Set ApplicationDisplayVersion in Inventory.Ui.csproj for
each release and increment ApplicationVersion as required by your packaging.
The example values above do not publish a release or change the app version.

The button opens the configured HTTPS download URL. The user installs the update
and restarts Inventory POS; this implementation does not silently install it.
Use the same package identity/publisher and test upgrading an existing installation
with its SQLite data before release. Do not uninstall or delete the data directory.

## Offline behavior

The request has a five-second timeout. On failure, the app uses its last valid
cached policy. With no cached policy, the app remains usable offline. A cached
mandatory policy keeps blocking offline. HTTP 204 clears the cached policy, so
disabling updates on the server can release a block on the next successful check.
The check happens at startup, not during a sale. Older app binaries without this
update mechanism cannot be forced to update by this client screen.
