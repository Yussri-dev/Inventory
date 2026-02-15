using Inventory.Dto.GlobalRequests.Results;
using Inventory.Services.Features.Pos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/pos")]
    [Authorize(Roles = "Cashier,Admin")]
    public class PosController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("bootstrap")]
        [ProducesResponseType(typeof(PosBootstrapResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Bootstrap(CancellationToken ct)
        {
            var result = await _mediator.Send(
                new PosBootstrapQuery(),
                ct
            );

            return Ok(result);
        }
    }
}
