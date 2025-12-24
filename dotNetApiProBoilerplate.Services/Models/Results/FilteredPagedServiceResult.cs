using Inventory.Services.Models.Core;

namespace Inventory.Services.Models.Results
{
    // Filtered and paged service result
    //
    // Purpose:
    // - Represents the result of a paginated query that also includes filter metadata
    // - Combines:
    //   - Data (entities)
    //   - Paging information
    //   - Sorting information (inherited)
    //   - Applied filter criteria
    //
    // Architectural role:
    // - Lives in the Services layer
    // - Designed for advanced search/query use cases
    // - Keeps filtering context available for consumers (API, UI, logging)
    //
    // Generic parameters:
    // - TEntity: type of the returned items
    // - TFilter: type describing the filter criteria that were applied
    public class FilteredPagedServiceResult<TEntity, TFilter>
        : PagedServiceResult<TEntity>
    {
        // Filter metadata
        //
        // Represents the filter object used for the query
        // Can be null if no filters were applied
        public TFilter? Filter { get; set; }
    }
}
