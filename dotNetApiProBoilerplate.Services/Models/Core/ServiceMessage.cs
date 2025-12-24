namespace Inventory.Services.Models.Core
{
    // Service-level message model
    //
    // Purpose:
    // - Represents a structured message returned by the service layer
    // - Can be used for:
    //   - Informational messages
    //   - Warnings
    //   - Errors
    //
    // Architectural intent:
    // - Complements the ServiceResult / ServiceResult<T> pattern
    // - Allows services to return rich, machine-readable messages
    // - Avoids relying solely on exceptions for non-fatal situations
    public class ServiceMessage
    {
        // Machine-readable message code
        //
        // Examples:
        // - "PRODUCT_CREATED"
        // - "EMAIL_ALREADY_EXISTS"
        // - "PASSWORD_WEAK"
        //
        // Intended for:
        // - Frontend logic
        // - Localization
        // - Analytics / logging
        public required string Code { get; set; }

        // Human-readable message
        //
        // Intended for:
        // - UI display
        // - Logs
        // - Debugging
        public required string Message { get; set; }

        // Priority / severity of the message
        //
        // Uses MessagePriority enum:
        // - Info
        // - Warning
        // - Error
        //
        // Enables:
        // - Conditional UI behavior
        // - Severity-based logging
        public MessagePriority MessagePriority { get; set; }
    }
}
