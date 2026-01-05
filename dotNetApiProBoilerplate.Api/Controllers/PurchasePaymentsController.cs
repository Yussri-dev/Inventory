using Inventory.Dto.PurchasePayments.Requests;
using Inventory.Dto.Queries;
using Inventory.Services.Features.PurchasePayments.Create;
using Inventory.Services.Features.PurchasePayments.Delete;
using Inventory.Services.Features.PurchasePayments.GetAll;
using Inventory.Services.Features.PurchasePayments.GetById;
using Inventory.Services.Features.PurchasePayments.Search;
using Inventory.Services.Features.PurchasePayments.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/purchasePayments")]

    [Authorize]
    public class PurchasePaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PurchasePaymentsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost]

        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreatePurchasePaymentRequest request,

            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new CreatePurchasePaymentCommand(request),
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
                new GetPurchasePaymentByIdQuery(id),
                cancellationToken
            );
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)] // List of purchasePayments
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var results = await _mediator.Send(
                new GetAllPurchasePaymentsQuery(),
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
            [FromBody] UpdatePurchasePaymentRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _mediator.Send(
                new UpdatePurchasePaymentCommand(id, request),
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
                new DeletePurchasePaymentCommand(id),
                cancellationToken
            );
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] PurchasePaymentQuery query,

            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new SearchPurchasePaymentsQuery(query),
                cancellationToken
            );
            return Ok(result);
        }
    }
}
