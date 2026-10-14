using Inventory.Dto.GlobalRequests.Results;
using Inventory.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/global-settings")]
    [Authorize(Roles = "SuperAdmin")]
    public sealed class GlobalSettingsController : ControllerBase
    {
        private readonly IGlobalSettingService _globalSettingService;

        public GlobalSettingsController(
            IGlobalSettingService globalSettingService)
        {
            _globalSettingService =
                globalSettingService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(GlobalConfigurationResult),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<GlobalConfigurationResult>> GetAsync(
            CancellationToken cancellationToken)
        {
            var result =
                await _globalSettingService.GetAsync(
                    cancellationToken);

            return Ok(result);
        }

        [HttpPut]
        [ProducesResponseType(
            typeof(GlobalConfigurationResult),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GlobalConfigurationResult>> UpdateAsync(
            [FromBody] UpdateGlobalConfigurationRequest request,
            CancellationToken cancellationToken)
        {
            Guid? userId = null;

            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (Guid.TryParse(
                    userIdValue,
                    out var parsedUserId))
            {
                userId =
                    parsedUserId;
            }

            var result =
                await _globalSettingService.UpdateAsync(
                    request,
                    userId,
                    cancellationToken);

            return Ok(result);
        }
    }
}
