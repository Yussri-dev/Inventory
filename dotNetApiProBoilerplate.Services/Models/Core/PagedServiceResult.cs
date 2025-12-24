namespace Inventory.Services.Models.Core
{
    // Generic paged service result
    //
    // Purpose:
    // - Represents the result of a paginated service-layer query
    // - Extends ServiceResult to include paging and sorting metadata
    // - Designed for list/search operations returning partial datasets
    //
    // Architectural role:
    // - Lives in the Services layer (not API)
    // - Keeps pagination logic out of controllers
    // - Can be mapped later to API DTOs (e.g. PagedResult<T>)
    //
    // Why it inherits from ServiceResult<IEnumerable<T>>:
    // - Wraps a collection of items as the primary data payload
    // - Preserves a consistent service result pattern
    public class PagedServiceResult<T> : ServiceResult<IEnumerable<T>>
    {
        // Total number of items available across all pages
        // Used to calculate total pages and navigation metadata
        public int TotalCount { get; set; }

        // Paging information (offset-based pagination)
        // Encapsulates limit/offset instead of page/pageSize
        public Paging paging { get; set; } = new Paging();

        // Sorting information
        // Typically represents the field(s) and direction used for sorting
        // Example: "CreatedAt DESC"
        public string? Sorting { get; set; }
    }
}
