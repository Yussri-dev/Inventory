using Inventory.Dto.CashCorrections.Results;
using MediatR;

namespace Inventory.Services.Features.CashCorrection.GetById
{
    public class GetCashCorrectionByIdQueryHandler
        : IRequestHandler<GetCashCorrectionByIdQuery, CashCorrectionResult>
    {
        private readonly CashCorrectionService _cashCorrectionService;

        public GetCashCorrectionByIdQueryHandler(CashCorrectionService customerService)
        {
            _cashCorrectionService = customerService;
        }

        public Task<CashCorrectionResult> Handle(GetCashCorrectionByIdQuery query, CancellationToken cancellationToken)
        {
            return _cashCorrectionService.GetByIdAsync(query.Id);
        }
    }
}
