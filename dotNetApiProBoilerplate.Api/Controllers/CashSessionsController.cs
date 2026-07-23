using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.CashSession.Close;
using Inventory.Services.Features.CashSession.Create;
using Inventory.Services.Features.CashSession.Delete;
using Inventory.Services.Features.CashSession.GetAll;
using Inventory.Services.Features.CashSession.GetById;
using Inventory.Services.Features.CashSession.Query;
using Inventory.Services.Features.CashSession.Search;
using Inventory.Services.Features.CashSession.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cashsessions")]
[Authorize]
public sealed class CashSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CashSessionsController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    // =========================
    // GET ACTIVE SERVER SESSION
    // =========================

    [HttpGet("active")]
    [ProducesResponseType(
        typeof(CashSessionResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    public async Task<ActionResult<CashSessionResult>> GetActive(
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new GetActiveCashSessionQuery(),
                cancellationToken);

        if (result == null)
        {
            return NoContent();
        }

        return Ok(result);
    }

    // =========================
    // CREATE / OPEN
    // =========================

    [HttpPost]
    [ProducesResponseType(
        typeof(CashSessionResult),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashSessionResult>> Create(
        [FromBody] CreateCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new CreateCashSessionCommand(
                    request),
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                version = "1.0",
                id = result.Id
            },
            result);
    }

    // =========================
    // GET BY ID
    // =========================

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(CashSessionResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashSessionResult>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new GetCashSessionByIdQuery(
                    id),
                cancellationToken);

        return Ok(result);
    }

    // =========================
    // GET ALL
    // =========================

    [HttpGet]
    [ProducesResponseType(
        typeof(List<CashSessionResult>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CashSessionResult>>> GetAll(
        CancellationToken cancellationToken)
    {
        var results =
            await _mediator.Send(
                new GetAllCashSessionsQuery(),
                cancellationToken);

        return Ok(results);
    }

    // =========================
    // UPDATE
    // =========================

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(CashSessionResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashSessionResult>> Update(
        Guid id,
        [FromBody] UpdateCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new UpdateCashSessionCommand(
                    id,
                    request),
                cancellationToken);

        return Ok(result);
    }

    // =========================
    // DELETE
    // =========================

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteCashSessionCommand(
                id),
            cancellationToken);

        return NoContent();
    }

    // =========================
    // SEARCH / QUERY
    // =========================

    [HttpGet("search")]
    [ProducesResponseType(
        typeof(PagedResult<CashSessionResult>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CashSessionResult>>> Search(
        [FromQuery] CashSessionQuery query,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new SearchCashSessionsQuery(
                    query),
                cancellationToken);

        return Ok(result);
    }

    // =========================
    // CLOSE
    // =========================

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(
        typeof(CashSessionResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashSessionResult>> Close(
        Guid id,
        [FromBody] CloseCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new CloseCashSessionCommand(
                    id,
                    request),
                cancellationToken);

        return Ok(result);
    }
}