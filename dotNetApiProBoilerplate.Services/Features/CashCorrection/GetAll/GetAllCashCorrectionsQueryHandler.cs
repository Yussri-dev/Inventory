using Inventory.Dto.CashCorrections.Results;
using MediatR;

namespace Inventory.Services.Features.CashCorrection.GetAll
{
    public class GetAllCashCorrectionsQueryHandler
        : IRequestHandler<GetAllCashCorrectionsQuery, List<CashCorrectionResult>>
    {
        private readonly CashCorrectionService _cashCorrectionService;

        public GetAllCashCorrectionsQueryHandler(CashCorrectionService cashCorrectionService)
        {
            _cashCorrectionService = cashCorrectionService;
        }

        public Task<List<CashCorrectionResult>> Handle(GetAllCashCorrectionsQuery query, CancellationToken cancellationToken)
        {
            return _cashCorrectionService.GetAllAsync();
        }
    }
}
