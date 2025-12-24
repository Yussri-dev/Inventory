using Inventory.Dto.CashCorrections.Results;
using MediatR;

namespace Inventory.Services.Features.CashCorrection.Create
{
    public class CreateCashCorrectionCommandHandler : IRequestHandler<CreateCashCorrectionCommand, CashCorrectionResult>
    {
        private readonly CashCorrectionService _cashCorrectionService;

        public CreateCashCorrectionCommandHandler(CashCorrectionService productService)
        {
            _cashCorrectionService = productService;
        }

        public Task<CashCorrectionResult> Handle(CreateCashCorrectionCommand command, CancellationToken cancellationToken)
        {
            return _cashCorrectionService.CreateAsync(command.Request);
        }
    }
}
