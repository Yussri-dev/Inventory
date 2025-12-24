namespace Inventory.Services.Models.Core
{
    // Paging metadata model
    //
    // Purpose:
    // - Encapsulate pagination parameters in a reusable structure
    // - Keeps pagination concerns explicit and testable
    //
    // Offset/Limit model is especially useful for:
    // - SQL queries
    // - Elasticsearch
    // - Cursor-based or hybrid pagination strategies
    public class Paging
    {
        // Number of items to skip before starting to collect results
        // Example: Offset = 20, Limit = 10 → items 21–30
        public int Offset { get; set; } = 0;

        // Maximum number of items to return
        // Acts as a safety limit to prevent large result sets
        public int Limit { get; set; } = 0;
    }
}
