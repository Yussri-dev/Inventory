using Inventory.Dto.Queries;
using Inventory.Dto.SupplierReturns.Requests;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Services.Features.SupplierReturns.Create;
using Inventory.Services.Features.SupplierReturns.Delete;
using Inventory.Services.Features.SupplierReturns.GetAll;
using Inventory.Services.Features.SupplierReturns.GetById;
using Inventory.Services.Features.SupplierReturns.Search;
using Inventory.Services.Features.SupplierReturns.Update;
using Inventory.Services.Features.SupplierReturns.CreateComplete;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/supplierReturns")]

    [Authorize]
    public class SupplierReturnsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SupplierReturnsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost]

        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateSupplierReturnRequest request,

            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new CreateSupplierReturnCommand(request),
                cancellationToken
            );
            return CreatedAtAction(
                nameof(GetById),
                new { version = "1.0", id = result.Id },
                result
            );
        }

        [HttpPost("complete")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateComplete(
            [FromBody] CreateCompleteSupplierReturnRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new CreateCompleteSupplierReturnCommand(request),
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
                new GetSupplierReturnByIdQuery(id),
                cancellationToken
            );
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)] // List of products
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var results = await _mediator.Send(
                new GetAllSupplierReturnsQuery(),
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
            [FromBody] UpdateSupplierReturnRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _mediator.Send(
                new UpdateSupplierReturnCommand(id, request),
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
                new DeleteSupplierReturnCommand(id),
                cancellationToken
            );
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] SupplierReturnQuery query,

            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new SearchSupplierReturnsQuery(query),
                cancellationToken
            );
            return Ok(result);
        }
    }
}
