using Inventory.Dto.CashReports.Results;
using MediatR;

namespace Inventory.Services.Features.CashReport.Create
{
    public class CreateCashReportCommandHandler : IRequestHandler<CreateCashReportCommand, CashReportResult>
    {
        private readonly CashReportService _cashReportService;

        public CreateCashReportCommandHandler(CashReportService reportService)
        {
            _cashReportService = reportService;
        }

        public Task<CashReportResult> Handle(CreateCashReportCommand command, CancellationToken cancellationToken)
        {
            return _cashReportService.CreateAsync(command.Request);
        }
    }
}
