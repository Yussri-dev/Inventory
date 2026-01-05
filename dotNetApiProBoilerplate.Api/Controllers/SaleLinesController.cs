using Inventory.Dto.SaleLines.Requests;
using Inventory.Dto.Queries;
using Inventory.Services.Features.SaleLines.Create;
using Inventory.Services.Features.SaleLines.Delete;
using Inventory.Services.Features.SaleLines.GetAll;
using Inventory.Services.Features.SaleLines.GetById;
using Inventory.Services.Features.SaleLines.Search;
using Inventory.Services.Features.SaleLines.Update;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/saleLines")]

    [Authorize]
    public class SaleLinesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SaleLinesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost]

        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateSaleLineRequest request,

            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new CreateSaleLineCommand(request),
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
                new GetSaleLineByIdQuery(id),
                cancellationToken
            );
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)] // List of returnLines
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var results = await _mediator.Send(
                new GetAllSaleLinesQuery(),
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
            [FromBody] UpdateSaleLineRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _mediator.Send(
                new UpdateSaleLineCommand(id, request),
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
                new DeleteSaleLineCommand(id),
                cancellationToken
            );
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] SaleLineQuery query,

            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new SearchSaleLinesQuery(query),
                cancellationToken
            );
            return Ok(result);
        }
    }
}
