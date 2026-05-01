using Inventory.Dto.ProductCategory.Requests;
using Inventory.Dto.Queries;
using Inventory.Services.Features.ProductCategory.Create;
using Inventory.Services.Features.ProductCategory.Delete;
using Inventory.Services.Features.ProductCategory.GetAll;
using Inventory.Services.Features.ProductCategory.GetById;
using Inventory.Services.Features.ProductCategory.Search;
using Inventory.Services.Features.ProductCategory.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/ProductCategory")]

    [Authorize]
    public class ProductCategoryController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProductCategoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductCategoryRequest request,

            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new CreateProductCategoryCommand(request),
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
                new GetProductCategoryByIdQuery(id),
                cancellationToken
            );
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var results = await _mediator.Send(
                new GetAllProductCategoryQuery(),
                cancellationToken
            );
            return Ok(results);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateProductCategoryRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _mediator.Send(
                new UpdateProductCategoryCommand(id, request),
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
                new DeleteProductCategoryCommand(id),
                cancellationToken
            );
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] ProductCategoryQuery query,

            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new SearchProductCategoryQuery(query),
                cancellationToken
            );
            return Ok(result);
        }
    }


}


