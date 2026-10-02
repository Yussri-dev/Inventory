using Inventory.Dto.AppUpdates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[ApiVersionNeutral]
[AllowAnonymous]
[Route("api/app-updates")]
public sealed class AppUpdatesController(
    IConfiguration configuration)
    : ControllerBase
{
    [HttpGet("windows")]
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
    public IActionResult Windows()
    {
        var settings =
            configuration.GetSection(
                "AppUpdates:Windows");

        if (!settings.GetValue<bool>("Enabled"))
            return NoContent();

        var manifest =
            settings.Get<AppUpdateManifest>();

        if (manifest == null ||
            !manifest.IsValid())
        {
            return Problem(
                detail:
                    "The Windows update configuration is invalid.",
                statusCode:
                    StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(manifest);
    }
}