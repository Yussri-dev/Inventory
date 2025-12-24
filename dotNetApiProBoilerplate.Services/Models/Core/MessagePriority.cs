namespace Inventory.Services.Models.Core
{
    // Enumeration representing the priority level of a message
    //
    // Purpose:
    // - Standardize how messages are classified across the service layer
    // - Enable consistent handling of informational messages, warnings, and errors
    // - Useful for logging, notifications, ServiceResult patterns, or UI feedback
    //
    // Why this enum exists:
    // - Avoids magic numbers or strings for severity levels
    // - Makes intent explicit and type-safe
    // - Can be reused across multiple layers (Services, API, UI)
    public enum MessagePriority
    {
        // Informational message
        // Used for non-critical events or successful operations
        Info = 0,

        // Warning message
        // Indicates a potential issue that does not block execution
        Warning = 1,

        // Error message
        // Indicates a failure or critical problem
        Error = 2
    }
}
