using Inventory.Dto.CashCorrections.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using MediatR;

namespace Inventory.Services.Features.CashCorrection.Search
{
    public class SearchCashCorrectionsQuery : IRequest<PagedResult<CashCorrectionResult>>
    {
        public CashCorrectionQuery Query { get; }

        public SearchCashCorrectionsQuery(CashCorrectionQuery query)
        {
            Query = query;
        }
    }
}
