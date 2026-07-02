using Inventory.Dto.Analytics.Results;
using Inventory.Dto.Sales.Results;
using Inventory.Services.Features.Analytics.Dashboard;
using Inventory.Services.Features.Analytics.Loss;
using Inventory.Services.Features.Analytics.Profit;
using Inventory.Services.Features.Analytics.WeeklyReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/analytics")]
    [Authorize]

    public class AnalyticsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AnalyticsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("profit")]
        [ProducesResponseType(typeof(SaleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfit(
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to)
        {
            var result = await _mediator.Send(
                new GetProfitAnalyticsQuery(from, to));
            return Ok(result);
        }

        [HttpGet("loss-products")]
        [ProducesResponseType(typeof(SaleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLossProducts(
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            [FromQuery] int limit = 10)
        {
            var result = await _mediator.Send(
                new GetLossProductsQuery(from, to, limit));
            return Ok(result);
        }

        [HttpGet("weekly")]
        [ProducesResponseType(typeof(SaleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Weekly(
        [FromQuery] int? year,
        [FromQuery] int? week)
        {
            var result = await _mediator.Send(
                new GetWeeklyReportQuery(year, week));
            return Ok(result);
        }

        [HttpGet("dashboard-summary")]
        [ProducesResponseType(typeof(DashboardSummaryResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardSummary(
    [FromQuery] DateOnly? from,
    [FromQuery] DateOnly? to)
        {
            var result = await _mediator.Send(
                new GetDashboardSummaryQuery(from, to));

            return Ok(result);
        }
    }
}
