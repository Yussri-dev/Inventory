using Inventory.Dto.CashReports.Results;
using MediatR;

namespace Inventory.Services.Features.CashReport.GetById
{
    public class GetCashReportByIdQueryHandler
        : IRequestHandler<GetCashReportByIdQuery, CashReportResult>
    {
        private readonly CashReportService _cashReportService;

        public GetCashReportByIdQueryHandler(CashReportService customerService)
        {
            _cashReportService = customerService;
        }

        public Task<CashReportResult> Handle(GetCashReportByIdQuery query, CancellationToken cancellationToken)
        {
            return _cashReportService.GetByIdAsync(query.Id);
        }
    }
}
