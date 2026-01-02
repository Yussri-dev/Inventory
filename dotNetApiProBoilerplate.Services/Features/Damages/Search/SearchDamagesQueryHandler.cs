using Inventory.Dto.Damages.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.Damages.Search
{
    public class SearchDamagesQueryHandler
    : IRequestHandler<SearchDamagesQuery, PagedResult<DamageResult>>
    {
        private readonly DamageService _customerService;

        public SearchDamagesQueryHandler(DamageService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<DamageResult>> Handle(SearchDamagesQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
