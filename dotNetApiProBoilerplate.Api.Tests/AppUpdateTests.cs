using Inventory.Api.Controllers;
using Inventory.Dto.AppUpdates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Inventory.Api.Tests;

public class AppUpdateTests
{
    [Theory]
    [InlineData("1.0", "1.0.0", "1.0.0", AppUpdateStatus.None)]
    [InlineData("1.0.0", "1.0.1", "1.0.0", AppUpdateStatus.Available)]
    [InlineData("1.0.0", "1.0.1", "1.0.1", AppUpdateStatus.Required)]
    [InlineData("1.0.1", "1.0.1", "1.0.0", AppUpdateStatus.None)]
    [InlineData("2.0.0", "1.0.1", "1.0.0", AppUpdateStatus.None)]
    [InlineData("1.9.0", "1.10.0", "1.0.0", AppUpdateStatus.Available)]
    public void ComparesNumericVersions(string installed, string latest, string minimum, AppUpdateStatus expected)
    {
        var manifest = new AppUpdateManifest { LatestVersion = latest, MinimumVersion = minimum, DownloadUrl = "https://example.com/pos.msix" };
        Assert.True(manifest.IsValid());
        Assert.Equal(expected, manifest.Compare(installed));
    }

    [Theory]
    [InlineData("bad", "1.0", "https://example.com/pos.msix")]
    [InlineData("1.0", "2.0", "https://example.com/pos.msix")]
    [InlineData("1.0", "1.0", "http://example.com/pos.msix")]
    [InlineData("1.0", "1.0", "https://user:password@example.com/pos.msix")]
    [InlineData("1.0", "1.0", "")]
    public void RejectsInvalidPolicy(string latest, string minimum, string url)
    {
        var manifest = new AppUpdateManifest { LatestVersion = latest, MinimumVersion = minimum, DownloadUrl = url };
        Assert.False(manifest.IsValid());
        Assert.Equal(AppUpdateStatus.None, manifest.Compare("0.9"));
    }

    [Fact]
    public void UnconfiguredEndpointIsDisabled()
    {
        Assert.IsType<NoContentResult>(new AppUpdatesController(new ConfigurationBuilder().Build()).Windows());
    }

    [Fact]
    public void EnabledInvalidPolicyReturnsServiceUnavailable()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppUpdates:Windows:Enabled"] = "true"
        }).Build();
        var result = Assert.IsType<ObjectResult>(new AppUpdatesController(config).Windows());
        Assert.Equal(503, result.StatusCode);
    }

    [Fact]
    public void EnabledEndpointReturnsValidatedPolicy()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppUpdates:Windows:Enabled"] = "true",
            ["AppUpdates:Windows:LatestVersion"] = "1.0.1",
            ["AppUpdates:Windows:MinimumVersion"] = "1.0.0",
            ["AppUpdates:Windows:DownloadUrl"] = "https://example.com/pos.msix"
        }).Build();
        var result = Assert.IsType<OkObjectResult>(new AppUpdatesController(config).Windows());
        Assert.Equal(AppUpdateStatus.Available, Assert.IsType<AppUpdateManifest>(result.Value).Compare("1.0.0"));
    }
}
