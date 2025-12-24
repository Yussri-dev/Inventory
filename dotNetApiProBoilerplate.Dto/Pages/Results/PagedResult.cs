namespace Inventory.Dto.Pages.Results
{
    // Generic DTO used to return paginated results from API endpoints
    // Commonly used for list, search, and query responses
    public class PagedResult<T>
    {
        // The collection of items for the current page
        // Generic type allows reuse across different entities and DTOs
        public List<T> Items { get; init; } = [];

        // Total number of items available across all pages
        // Used by clients to calculate total pages
        public int TotalCount { get; init; }

        // Current page number (1-based or 0-based depending on convention)
        public int Page { get; init; }

        // Number of items per page
        // Determines the size of each paginated slice
        public int PageSize { get; init; }
    }
}
