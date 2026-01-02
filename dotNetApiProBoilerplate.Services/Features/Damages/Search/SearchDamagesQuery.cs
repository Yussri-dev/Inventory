using Inventory.Dto.Damages.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using MediatR;

namespace Inventory.Services.Features.Damages.Search
{
    public class SearchDamagesQuery : IRequest<PagedResult<DamageResult>>
    {
        public DamageQuery Query { get; }

        public SearchDamagesQuery(DamageQuery query)
        {
            Query = query;
        }
    }
}
