using Inventory.Dto.CashReports.Results;
using MediatR;

namespace Inventory.Services.Features.CashReport.GetAll
{
    public class GetAllCashReportsQueryHandler
       : IRequestHandler<GetAllCashReportsQuery, List<CashReportResult>>
    {
        private readonly CashReportService _cashReportService;

        public GetAllCashReportsQueryHandler(CashReportService cashReportService)
        {
            _cashReportService = cashReportService;
        }

        public Task<List<CashReportResult>> Handle(GetAllCashReportsQuery query, CancellationToken cancellationToken)
        {
            return _cashReportService.GetAllAsync();
        }
    }
}
