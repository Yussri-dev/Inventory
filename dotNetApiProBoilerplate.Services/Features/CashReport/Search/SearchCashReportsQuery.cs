using Inventory.Dto.CashReports.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.CashReport.Search;
using MediatR;

namespace Inventory.Services.Features.CashReport.Search
{
    
    public class SearchCashReportsQuery : IRequest<PagedResult<CashReportResult>>
    {
        public CashReportQuery Query { get; }

        public SearchCashReportsQuery(CashReportQuery query)
        {
            Query = query;
        }
    }
}
