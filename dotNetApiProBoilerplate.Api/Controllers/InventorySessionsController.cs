using Inventory.Dto.InventorySessions.Requests;
using Inventory.Dto.InventorySessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.InventorySessions.Close;
using Inventory.Services.Features.InventorySessions.Create;
using Inventory.Services.Features.InventorySessions.Delete;
using Inventory.Services.Features.InventorySessions.GetAll;
using Inventory.Services.Features.InventorySessions.GetById;
using Inventory.Services.Features.InventorySessions.Search;
using Inventory.Services.Features.InventorySessions.Update;
using Inventory.Services.Features.InventorySessions.Validate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/inventorySessions")]
[Authorize]
public sealed class InventorySessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventorySessionsController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    // =========================
    // CREATE
    // =========================

    [HttpPost]
    [ProducesResponseType(
        typeof(InventorySessionResult),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventorySessionResult>> Create(
        [FromBody] CreateInventorySessionRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new CreateInventorySessionCommand(
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
        typeof(InventorySessionResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventorySessionResult>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new GetInventorySessionByIdQuery(
                    id),
                cancellationToken);

        return Ok(result);
    }

    // =========================
    // GET ALL
    // =========================

    [HttpGet]
    [ProducesResponseType(
        typeof(List<InventorySessionResult>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<InventorySessionResult>>> GetAll(
        CancellationToken cancellationToken)
    {
        var results =
            await _mediator.Send(
                new GetAllInventorySessionsQuery(),
                cancellationToken);

        return Ok(results);
    }

    // =========================
    // UPDATE
    // =========================

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(InventorySessionResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventorySessionResult>> Update(
        Guid id,
        [FromBody] UpdateInventorySessionRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new UpdateInventorySessionCommand(
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
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteInventorySessionCommand(
                id),
            cancellationToken);

        return NoContent();
    }

    // =========================
    // CLOSE
    // =========================

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new CloseInventorySessionCommand(
                id),
            cancellationToken);

        return NoContent();
    }

    // =========================
    // VALIDATE
    // =========================

    [HttpPost("{id:guid}/validate")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Validate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ValidateInventorySessionCommand(
                id),
            cancellationToken);

        return NoContent();
    }

    // =========================
    // SEARCH
    // =========================

    [HttpGet("search")]
    [ProducesResponseType(
        typeof(PagedResult<InventorySessionResult>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<InventorySessionResult>>> Search(
        [FromQuery] InventorySessionQuery query,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new SearchInventorySessionsQuery(
                    query),
                cancellationToken);

        return Ok(result);
    }
}