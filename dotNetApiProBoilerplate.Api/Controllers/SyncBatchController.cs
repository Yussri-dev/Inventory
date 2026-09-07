using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/sync")]
    public sealed class SyncBatchController
       : ControllerBase
    {
        private const int MaximumRequestSize =
            20 * 1024 * 1024;

        private readonly ISyncBatchService _syncBatchService;

        public SyncBatchController(
            ISyncBatchService syncBatchService)
        {
            _syncBatchService =
                syncBatchService;
        }

        [HttpPost("batches")]
        [RequestSizeLimit(MaximumRequestSize)]
        [ProducesResponseType(
            typeof(SyncBatchResult),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status413PayloadTooLarge)]
        public async Task<ActionResult<SyncBatchResult>>
            UploadBatch(
                [FromBody] SyncBatchRequest request,
                CancellationToken cancellationToken)
        {
            var result =
                await _syncBatchService.ProcessAsync(
                    request,
                    cancellationToken);

            return Ok(
                result);
        }
    }
}
