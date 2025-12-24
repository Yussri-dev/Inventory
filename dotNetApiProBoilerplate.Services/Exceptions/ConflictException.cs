namespace Inventory.Services.Exceptions
{
    // Exception representing a conflict error (HTTP 409)
    //
    // Purpose:
    // - Thrown when a request cannot be completed due to a conflict
    //   with the current state of the resource
    // - Common examples:
    //   - Duplicate unique values (email, product name, code)
    //   - Versioning or concurrency conflicts
    //
    // Why a custom exception:
    // - Makes intent explicit in service-layer code
    // - Avoids using generic Exception for business-rule violations
    // - Enables clean mapping to HTTP status codes in middleware
    //
    // How it is used in the architecture:
    // - Thrown in Services layer
    // - Caught by ExceptionHandlingMiddleware
    // - Translated into:
    //   - HTTP 409 Conflict
    //   - Standardized JSON error response
    public class ConflictException : Exception
    {
        // Constructor accepting a descriptive error message
        // Passed directly to the base Exception class
        public ConflictException(string message) : base(message) { }
    }
}
