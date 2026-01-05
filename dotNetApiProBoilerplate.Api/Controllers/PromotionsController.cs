using Inventory.Dto.Promotions.Requests;
using Inventory.Dto.Queries;
using Inventory.Services.Features.Promotions.Create;
using Inventory.Services.Features.Promotions.Delete;
using Inventory.Services.Features.Promotions.GetAll;
using Inventory.Services.Features.Promotions.GetById;
using Inventory.Services.Features.Promotions.Search;
using Inventory.Services.Features.Promotions.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/promotions")]

    [Authorize]
    public class PromotionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PromotionsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost]

        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreatePromotionRequest request,

            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new CreatePromotionCommand(request),
                cancellationToken
            );
            return CreatedAtAction(
                nameof(GetById),
                new { version = "1.0", id = result.Id },
                result
            );
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetPromotionByIdQuery(id),
                cancellationToken
            );
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)] // List of products
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var results = await _mediator.Send(
                new GetAllPromotionsQuery(),
                cancellationToken
            );
            return Ok(results);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)] // Updated
        [ProducesResponseType(StatusCodes.Status400BadRequest)]          // Invalid payload
        [ProducesResponseType(StatusCodes.Status404NotFound)]            // Not found
        [ProducesResponseType(StatusCodes.Status409Conflict)]            // Conflict
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdatePromotionRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _mediator.Send(
                new UpdatePromotionCommand(id, request),
                cancellationToken
            );
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Deleted
        [ProducesResponseType(StatusCodes.Status404NotFound)]  // Not found
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new DeletePromotionCommand(id),
                cancellationToken
            );
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] PromotionQuery query,

            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new SearchPromotionsQuery(query),
                cancellationToken
            );
            return Ok(result);
        }
    }
}
