namespace Inventory.Services.Models.Core
{
    // Sorting metadata model
    //
    // Purpose:
    // - Represents sorting instructions in a structured, type-safe way
    // - Decouples sorting logic from raw strings
    // - Used by services and repositories to apply ordering dynamically
    //
    // Architectural role:
    // - Lives in the Services layer
    // - Can be mapped from API query DTOs (e.g. ProductQuery)
    // - Prevents hard-coded sorting logic in controllers
    public class Sorting
    {
        // Name of the property to sort by
        //
        // Example:
        // - "CreatedAt"
        // - "Name"
        // - "Price"
        //
        // Must match a valid property on the target entity or DTO
        public string PropertyName { get; set; } = string.Empty;

        // Sorting direction flag
        //
        // true  => descending order
        // false => ascending order
        public bool IsDescending { get; set; }
    }
}
