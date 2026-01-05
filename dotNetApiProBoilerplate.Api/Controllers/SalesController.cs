using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;
using Inventory.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/sales")]

    [Authorize]
    public class SalesController : ControllerBase
    {
        private readonly SaleService _saleService;

        public SalesController(SaleService saleService)
        {
            _saleService = saleService;
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost]
        [ProducesResponseType(typeof(SaleResult), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateSaleRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _saleService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { version = "1.0", id = result.Id },
                result
            );
        }

        // =========================
        // CREATE COMPLETE
        // =========================
        [HttpPost("complete")]
        [ProducesResponseType(typeof(SaleResult), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateComplete(
            [FromBody] CreateCompleteSaleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _saleService.CreateCompleteAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { version = "1.0", id = result.Id },
                result
            );
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(SaleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _saleService.GetByIdAsync(id);
            return Ok(result);
        }

        // =========================
        // GET ALL
        // =========================
        [HttpGet]
        [ProducesResponseType(typeof(List<SaleResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _saleService.GetAllAsync();
            return Ok(result);
        }

        // =========================
        // UPDATE
        // =========================
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(SaleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateSaleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _saleService.UpdateAsync(id, request);
            return Ok(result);
        }

        // =========================
        // DELETE (SOFT)
        // =========================
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _saleService.DeleteAsync(id);
            return NoContent();
        }

        // =========================
        // QUERY (pagination / search / sorting)
        // =========================
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<SaleResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Query(
            [FromQuery] SaleQuery query)
        {
            var result = await _saleService.QueryAsync(query);
            return Ok(result);
        }
    }
}
