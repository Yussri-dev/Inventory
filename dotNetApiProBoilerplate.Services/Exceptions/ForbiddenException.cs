// Base .NET namespaces
// Included for consistency across custom exception files
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Exceptions
{
    // Exception representing a forbidden access error (HTTP 403)
    //
    // Purpose:
    // - Thrown when a user is authenticated
    //   but does not have permission to perform an action
    //
    // Common scenarios:
    // - Accessing a resource owned by another user
    // - Missing required role or permission
    // - Violating authorization business rules
    //
    // Why a custom exception:
    // - Makes authorization failures explicit in the service layer
    // - Separates authentication (401) from authorization (403)
    // - Enables clean HTTP status mapping via middleware
    //
    // How it fits in the architecture:
    // - Thrown inside Services layer
    // - Caught by ExceptionHandlingMiddleware
    // - Translated into HTTP 403 Forbidden with a structured error response
    public class ForbiddenException : Exception
    {
        // Constructor accepting a descriptive error message
        // Passed directly to the base Exception class
        public ForbiddenException(string message) : base(message) { }
    }
}
