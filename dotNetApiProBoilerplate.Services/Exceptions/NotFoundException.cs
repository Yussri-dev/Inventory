// Base .NET namespaces
// Included for consistency across custom exception files
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Exceptions
{
    // Exception representing a not-found error (HTTP 404)
    //
    // Purpose:
    // - Thrown when a requested resource does not exist
    // - Represents a missing entity in the domain or persistence layer
    //
    // Why a custom exception:
    // - Makes absence of data an explicit business case
    // - Avoids returning nulls that propagate silently
    // - Enables deterministic mapping to HTTP 404 via middleware
    //
    // How it is used in the architecture:
    // - Thrown in Services layer when an entity lookup fails
    // - Caught by ExceptionHandlingMiddleware
    // - Translated into a standardized 404 JSON error response
    public class NotFoundException : Exception
    {
        // Constructor accepting a custom error message
        // Used when the message is already known or composed elsewhere
        public NotFoundException(string message) : base(message) { }

        // Constructor generating a standardized error message
        // Commonly used for entity-based lookups
        //
        // Parameters:
        // - name: entity name (e.g. "Product", "User")
        // - key: identifier value used in the lookup
        //
        // Example output:
        // "Entity 'Product' with key '123' was not found."
        public NotFoundException(string name, object key)
            : base($"Entity '{name}' with key '{key}' was not found.") { }
    }
}
