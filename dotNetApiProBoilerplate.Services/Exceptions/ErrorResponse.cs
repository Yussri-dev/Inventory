namespace Inventory.Services.Exceptions
{
    // Standardized error response model
    //
    // Purpose:
    // - Defines a consistent JSON structure for API error responses
    // - Returned by the global ExceptionHandlingMiddleware
    // - Inspired by RFC 7807 (Problem Details) with custom extensions
    //
    // Why this class exists:
    // - Ensures all errors share the same shape
    // - Makes frontend error handling predictable
    // - Decouples error formatting from controllers and services
    //
    // Typical usage flow:
    // - Service throws a domain-specific exception
    // - Middleware catches the exception
    // - ErrorResponse is populated and serialized to JSON
    //
    // Field meanings:
    public class ErrorResponse
    {
        // HTTP status code (e.g. 400, 401, 404, 409, 500)
        public int Status { get; set; }

        // Short, human-readable summary of the error
        // Example: "Validation Error", "Resource Not Found"
        public string Title { get; set; } = string.Empty;

        // Detailed error message
        // Intended for developers or UI display
        public string Detail { get; set; } = string.Empty;

        // Request path that caused the error
        // Useful for debugging and logging
        public string Instance { get; set; } = string.Empty;

        // Unique trace identifier for the request
        // Allows correlation between logs and client errors
        public string TraceId { get; set; } = string.Empty;

        // Stack trace of the exception
        // Populated only in Development environment
        public string? StackTrace { get; set; }

        // Validation errors grouped by field name
        // Example:
        // {
        //   "Email": ["Email is required", "Email format is invalid"],
        //   "Password": ["Password must be at least 8 characters"]
        // }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
