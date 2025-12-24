using Inventory.Dto.CashCorrections.Results;
using MediatR;

namespace Inventory.Services.Features.CashCorrection.Update
{
    public class UpdateCashCorrectionCommandHandler
       : IRequestHandler<UpdateCashCorrectionCommand, CashCorrectionResult>
    {
        private readonly CashCorrectionService _cashCorrectionService;

        public UpdateCashCorrectionCommandHandler(CashCorrectionService customerService)
        {
            _cashCorrectionService = customerService;
        }

        public Task<CashCorrectionResult> Handle(UpdateCashCorrectionCommand command, CancellationToken cancellationToken)
        {
            return _cashCorrectionService.UpdateAsync(command.Id, command.Request);
        }
    }
}
