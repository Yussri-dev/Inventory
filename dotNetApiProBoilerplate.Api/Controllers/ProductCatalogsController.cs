using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Dto.Queries;
using Inventory.Services.Features.ProductCatalogs.Create;
using Inventory.Services.Features.ProductCatalogs.Delete;
using Inventory.Services.Features.ProductCatalogs.GetAll;
using Inventory.Services.Features.ProductCatalogs.GetById;
using Inventory.Services.Features.ProductCatalogs.Search;
using Inventory.Services.Features.ProductCatalogs.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/productcatalogs")]
    [Authorize] // all endpoints require auth
    public class ProductCatalogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductCatalogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // =========================
        // CREATE – SuperAdmin only
        // =========================
        [HttpPost]
        ////[Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductCatalogRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new CreateProductCatalogCommand(request),
                cancellationToken
            );

            return CreatedAtAction(
                nameof(GetById),
                new { version = "1.0", id = result.Id },
                result
            );
        }

        // =========================
        // GET BY ID – Global read
        // =========================
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetProductCatalogByIdQuery(id),
                cancellationToken
            );

            return Ok(result);
        }

        // =========================
        // GET ALL – Global read
        // =========================
        [HttpGet]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var results = await _mediator.Send(
                new GetAllProductCatalogsQuery(),
                cancellationToken
            );

            return Ok(results);
        }

        // =========================
        // UPDATE – SuperAdmin only
        // =========================
        [HttpPut("{id:guid}")]
        //[Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateProductCatalogRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new UpdateProductCatalogCommand(id, request),
                cancellationToken
            );

            return Ok(result);
        }

        // =========================
        // DELETE – SuperAdmin only
        // =========================
        [HttpDelete("{id:guid}")]
        //[Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new DeleteProductCatalogCommand(id),
                cancellationToken
            );

            return NoContent();
        }

        // =========================
        // SEARCH – Global read
        // =========================
        [HttpGet("search")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] ProductCatalogQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new SearchProductCatalogsQuery(query),
                cancellationToken
            );

            return Ok(result);
        }
    }
}
