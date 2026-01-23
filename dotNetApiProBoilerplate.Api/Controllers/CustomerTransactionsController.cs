using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.Queries;
using Inventory.Services.Features.CustomerTransactions.Balance;
using Inventory.Services.Features.CustomerTransactions.Create;
using Inventory.Services.Features.CustomerTransactions.Delete;
using Inventory.Services.Features.CustomerTransactions.GetAll;
using Inventory.Services.Features.CustomerTransactions.GetById;
using Inventory.Services.Features.CustomerTransactions.RegisterPayment;
using Inventory.Services.Features.CustomerTransactions.RegisterRefund;
using Inventory.Services.Features.CustomerTransactions.Search;
using Inventory.Services.Features.CustomerTransactions.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/customertransactions")]

    [Authorize]
    public class CustomerTransactionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CustomerTransactionsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost]

        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateCustomerTransactionRequest request,

            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new CreateCustomerTransactionCommand(request),
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
                new GetCustomerTransactionByIdQuery(id),
                cancellationToken
            );
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)] // List of products
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var results = await _mediator.Send(
                new GetAllCustomerTransactionsQuery(),
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
            [FromBody] UpdateCustomerTransactionRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _mediator.Send(
                new UpdateCustomerTransactionCommand(id, request),
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
                new DeleteCustomerTransactionCommand(id),
                cancellationToken
            );
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] CustomerTransactionQuery query,

            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new SearchCustomerTransactionsQuery(query),
                cancellationToken
            );
            return Ok(result);
        }

        [HttpPost("register-payment")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterPayment(
                    [FromBody] RegisterCustomerPaymentRequest request,
                    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new RegisterCustomerPaymentCommand(request),
                cancellationToken
            );

            return CreatedAtAction(
                nameof(GetById),
                new { version = "1.0", id = result.Id },
                result
            );
        }

        [HttpGet("customers-with-balance")]
        [ProducesResponseType(typeof(List<CustomerCreditResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCustomersWithBalance(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetCustomersWithBalanceQuery(),
                cancellationToken
            );
            return Ok(result);
        }

        [HttpPost("register-refund")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterRefund(
            [FromBody] RegisterCustomerRefundRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new RegisterCustomerRefundCommand(request),
                cancellationToken
            );

            return CreatedAtAction(
                nameof(GetById),
                new { version = "1.0", id = result.Id },
                result
            );
        }
    }
}
