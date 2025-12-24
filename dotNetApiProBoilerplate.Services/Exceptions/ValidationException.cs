// Base .NET namespaces
namespace Inventory.Services.Exceptions
{
    // Exception representing a validation failure (HTTP 400)
    //
    // Purpose:
    // - Thrown when input data or business rules fail validation
    // - Carries structured validation errors back to the API layer
    //
    // Why a custom exception:
    // - Separates validation failures from other error types
    // - Avoids mixing business-rule validation with ModelState
    // - Enables rich, field-level error reporting
    //
    // How it fits in the architecture:
    // - Thrown in Services layer when validation fails
    // - Caught by ExceptionHandlingMiddleware
    // - Translated into HTTP 400 Bad Request with detailed error payload
    public class ValidationException : Exception
    {
        // Collection of validation errors grouped by field name
        // Example:
        // {
        //   "Email": ["Email is required"],
        //   "Password": ["Password must be at least 8 characters"]
        // }
        public Dictionary<string, string[]> Errors { get; }

        // Constructor accepting structured validation errors
        // Used when multiple field-level errors are present
        public ValidationException(Dictionary<string, string[]> errors)
            : base("One or more validation errors occurred.")
        {
            Errors = errors;
        }

        // Constructor accepting a single validation message
        // Useful for simple validation failures
        public ValidationException(string message) : base(message)
        {
            Errors = new Dictionary<string, string[]>();
        }
    }
}
