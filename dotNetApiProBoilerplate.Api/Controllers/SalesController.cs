using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;
using Inventory.Services;
using Inventory.Services.Abstractions;
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
        private readonly ITicketFormatter _ticketFormatter;
        public SalesController(SaleService saleService, ITicketFormatter ticketFormatter)
        {
            _saleService = saleService;
            _ticketFormatter = ticketFormatter;
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

        [HttpPost("Pending")]
        [ProducesResponseType(typeof(SaleResult), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreatePending(
            [FromBody] CreatePendingSaleRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _saleService.CreatePendingAsync(request);

            return Ok(result);
        }

        [HttpGet("pending/{id}")]
        public async Task<IActionResult> GetPendingById(Guid id)
        {
            var result = await _saleService.GetPendingByIdAsync(id);
            return Ok(result);
        }
        //[HttpGet("{id:guid}/ticket")]
        //public async Task<IActionResult> PrintTicket(Guid id)
        //{
        //    var ticket = await _saleService.BuildTicketAsync(id);
        //    var pdf = _ticketFormatter.Format(ticket);

        //    return File(pdf, "application/pdf", $"{ticket.InvoiceNumber}.pdf");
        //}


        // =========================
        // CREATE COMPLETE
        // =========================
        [HttpPost("complete")]
        [ProducesResponseType(typeof(SaleResult), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateComplete([FromBody] CreateCompleteSaleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var sale = await _saleService.CreateCompleteAsync(request);
            var ticket = await _saleService.BuildTicketAsync(sale.Id);
            var pdf = _ticketFormatter.Format(ticket);

            return Ok(new CreateCompleteSaleResult
            {
                Sale = sale,
                Ticket = ticket,
                PdfBase64 = Convert.ToBase64String(pdf)
            });
        }

        [HttpGet("{id:guid}/ticket")]
        public async Task<IActionResult> GetTicket(Guid id)
        {
            var ticket = await _saleService.BuildTicketAsync(id);
            var pdf = _ticketFormatter.Format(ticket);

            return File(pdf, "application/pdf", $"{ticket.InvoiceNumber}.pdf");
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
        // Get Sale By Customer
        // =========================
        [HttpGet("by-customer/{customerId:guid}")]
        [ProducesResponseType(typeof(PagedResult<SaleResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByCustomer(
            Guid customerId,
            [FromQuery] CustomerSaleQuery query)
        {
            if (customerId == Guid.Empty)
                return BadRequest("Invalid customer id.");

            query.CustomerId = customerId;

            var result = await _saleService.GetByCustomerAsync(query);

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
        // GET ALL PENDING
        // =========================
        [HttpGet("pending")]
        [ProducesResponseType(typeof(List<SaleResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingAsync()
        {
            var result = await _saleService.GetPendingAsync();
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

        [HttpPut("pending/{id:guid}")]
        public async Task<IActionResult> UpdatePending(
    Guid id,
    [FromBody] CreatePendingSaleRequest request)
        {
            var result = await _saleService.UpdatePendingAsync(id, request);
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

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] SaleHistoryQuery query)
        {
            var result = await _saleService.GetHistoryAsync(query);
            return Ok(result);
        }

    }
}
