using MediatR;

namespace Inventory.Services.Features.CashReport.Delete
{
    public class DeleteCashReportCommandHandler
         : IRequestHandler<DeleteCashReportCommand, Unit>
    {
        private readonly CashReportService _cashReportService;

        public DeleteCashReportCommandHandler(CashReportService cashReportService)
        {
            _cashReportService = cashReportService;
        }

        public async Task<Unit> Handle(DeleteCashReportCommand command, CancellationToken cancellationToken)
        {
            await _cashReportService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
