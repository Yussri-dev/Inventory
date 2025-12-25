using Inventory.Dto.CashReports.Results;
using MediatR;

namespace Inventory.Services.Features.CashReport.Update
{
    public class UpdateCashReportCommandHandler
       : IRequestHandler<UpdateCashReportCommand, CashReportResult>
    {
        private readonly CashReportService _cashReportService;

        public UpdateCashReportCommandHandler(CashReportService customerService)
        {
            _cashReportService = customerService;
        }

        public Task<CashReportResult> Handle(UpdateCashReportCommand command, CancellationToken cancellationToken)
        {
            return _cashReportService.UpdateAsync(command.Id, command.Request);
        }
    }
}
