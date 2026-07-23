using Inventory.Dto.InventoryLines.Requests;
using Inventory.Dto.InventoryLines.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.InventoryLines.Create;
using Inventory.Services.Features.InventoryLines.Delete;
using Inventory.Services.Features.InventoryLines.GetAll;
using Inventory.Services.Features.InventoryLines.GetById;
using Inventory.Services.Features.InventoryLines.Search;
using Inventory.Services.Features.InventoryLines.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/inventoryLines")]
[Authorize]
public sealed class InventoryLinesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryLinesController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(InventoryLineResult),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryLineResult>> Create(
        [FromBody] CreateInventoryLineRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new CreateInventoryLineCommand(
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(InventoryLineResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryLineResult>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new GetInventoryLineByIdQuery(
                    id),
                cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(List<InventoryLineResult>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<InventoryLineResult>>> GetAll(
        CancellationToken cancellationToken)
    {
        var results =
            await _mediator.Send(
                new GetAllInventoryLinesQuery(),
                cancellationToken);

        return Ok(results);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(InventoryLineResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryLineResult>> Update(
        Guid id,
        [FromBody] UpdateInventoryLineRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new UpdateInventoryLineCommand(
                    id,
                    request),
                cancellationToken);

        return Ok(result);
    }

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
            new DeleteInventoryLineCommand(
                id),
            cancellationToken);

        return NoContent();
    }

    [HttpGet("search")]
    [ProducesResponseType(
        typeof(PagedResult<InventoryLineResult>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<InventoryLineResult>>> Search(
        [FromQuery] InventoryLineQuery query,
        CancellationToken cancellationToken)
    {
        var result =
            await _mediator.Send(
                new SearchInventoryLinesQuery(
                    query),
                cancellationToken);

        return Ok(result);
    }
}