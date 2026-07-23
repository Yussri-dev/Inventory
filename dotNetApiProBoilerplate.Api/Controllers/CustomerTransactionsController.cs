using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.CustomerTransactions.Balance;
using Inventory.Services.Features.CustomerTransactions.Create;
using Inventory.Services.Features.CustomerTransactions.CustomerDetails;
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

namespace Inventory.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customertransactions")]
[Authorize]
public sealed class CustomerTransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomerTransactionsController(
        IMediator mediator)
    {
        _mediator =
            mediator;
    }

    // =========================
    // CREATE GENERIC TRANSACTION
    // =========================
    [HttpPost]
    [ProducesResponseType(
        typeof(CustomerTransactionResult),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerTransactionResult>> Create(
        [FromBody] CreateCustomerTransactionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        var result =
            await _mediator.Send(
                new CreateCustomerTransactionCommand(
                    request),
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                version =
                    "1.0",

                id =
                    result.Id
            },
            result);
    }

    // =========================
    // GET BY ID
    // =========================
    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(CustomerTransactionResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerTransactionResult>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id ==
            Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(id),
                "Customer transaction id is required.");

            return ValidationProblem(
                ModelState);
        }

        var result =
            await _mediator.Send(
                new GetCustomerTransactionByIdQuery(
                    id),
                cancellationToken);

        return Ok(
            result);
    }

    // =========================
    // GET ALL
    // =========================
    [HttpGet]
    [ProducesResponseType(
        typeof(List<CustomerTransactionResult>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<CustomerTransactionResult>>> GetAll(
        CancellationToken cancellationToken)
    {
        var results =
            await _mediator.Send(
                new GetAllCustomerTransactionsQuery(),
                cancellationToken);

        return Ok(
            results);
    }

    // =========================
    // UPDATE
    // =========================
    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(CustomerTransactionResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerTransactionResult>> Update(
        Guid id,
        [FromBody] UpdateCustomerTransactionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (id ==
            Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(id),
                "Customer transaction id is required.");

            return ValidationProblem(
                ModelState);
        }

        /*
         * The route id is authoritative.
         * Do not trust a different id sent in the request body.
         */
        request.Id =
            id;

        var result =
            await _mediator.Send(
                new UpdateCustomerTransactionCommand(
                    id,
                    request),
                cancellationToken);

        return Ok(
            result);
    }

    // =========================
    // DELETE
    // =========================
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id ==
            Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(id),
                "Customer transaction id is required.");

            return ValidationProblem(
                ModelState);
        }

        await _mediator.Send(
            new DeleteCustomerTransactionCommand(
                id),
            cancellationToken);

        return NoContent();
    }

    // =========================
    // SEARCH
    // =========================
    [HttpGet("search")]
    [ProducesResponseType(
        typeof(PagedResult<CustomerTransactionResult>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<CustomerTransactionResult>>>
        Search(
            [FromQuery] CustomerTransactionQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        var result =
            await _mediator.Send(
                new SearchCustomerTransactionsQuery(
                    query),
                cancellationToken);

        return Ok(
            result);
    }

    // =========================
    // REGISTER PAYMENT
    // =========================
    [HttpPost("register-payment")]
    [ProducesResponseType(
        typeof(CustomerTransactionResult),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerTransactionResult>>
        RegisterPayment(
            [FromBody] RegisterCustomerPaymentRequest request,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        var result =
            await _mediator.Send(
                new RegisterCustomerPaymentCommand(
                    request),
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                version =
                    "1.0",

                id =
                    result.Id
            },
            result);
    }

    // =========================
    // REGISTER REFUND
    // =========================
    [HttpPost("register-refund")]
    [ProducesResponseType(
        typeof(CustomerTransactionResult),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerTransactionResult>>
        RegisterRefund(
            [FromBody] RegisterCustomerRefundRequest request,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        var result =
            await _mediator.Send(
                new RegisterCustomerRefundCommand(
                    request),
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                version =
                    "1.0",

                id =
                    result.Id
            },
            result);
    }

    // =========================
    // CUSTOMERS WITH BALANCE
    // =========================
    [HttpGet("customers-with-balance")]
    [ProducesResponseType(
        typeof(List<CustomerCreditResult>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<CustomerCreditResult>>>
        GetCustomersWithBalance(
            CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new GetCustomersWithBalanceQuery(),
                cancellationToken);

        return Ok(
            result);
    }

    // =========================
    // CUSTOMER DETAIL
    // =========================
    [HttpGet("customer-detail/{customerId:guid}")]
    [ProducesResponseType(
        typeof(CustomerDetailResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailResult>>
        GetCustomerDetail(
            Guid customerId,
            CancellationToken cancellationToken)
    {
        if (customerId ==
            Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(customerId),
                "Customer id is required.");

            return ValidationProblem(
                ModelState);
        }

        var result =
            await _mediator.Send(
                new GetCustomerDetailQuery(
                    customerId),
                cancellationToken);

        return Ok(
            result);
    }
}