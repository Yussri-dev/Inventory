using Inventory.Dto.CashCorrections.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.CashCorrection.Search
{
    public class SearchCashCorrectionsQueryHandler
    : IRequestHandler<SearchCashCorrectionsQuery, PagedResult<CashCorrectionResult>>
    {
        private readonly CashCorrectionService _cashCorrectionService;

        public SearchCashCorrectionsQueryHandler(CashCorrectionService customerService)
        {
            _cashCorrectionService = customerService;
        }

        public Task<PagedResult<CashCorrectionResult>> Handle(SearchCashCorrectionsQuery query, CancellationToken cancellationToken)
        {
            return _cashCorrectionService.QueryAsync(query.Query);
        }
    }
}
