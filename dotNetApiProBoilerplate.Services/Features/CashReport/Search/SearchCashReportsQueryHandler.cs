using Inventory.Dto.CashReports.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.CashReport.Search
{
    public class SearchCashReportsQueryHandler
    : IRequestHandler<SearchCashReportsQuery, PagedResult<CashReportResult>>
    {
        private readonly CashReportService _cashReportService;

        public SearchCashReportsQueryHandler(CashReportService customerService)
        {
            _cashReportService = customerService;
        }

        public Task<PagedResult<CashReportResult>> Handle(SearchCashReportsQuery query, CancellationToken cancellationToken)
        {
            return _cashReportService.QueryAsync(query.Query);
        }
    }
}
