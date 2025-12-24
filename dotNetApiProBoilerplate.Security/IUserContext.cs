// Base .NET namespaces
// Included for consistency across security abstractions
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Security
{
    // Generic user context abstraction
    //
    // Purpose:
    // - Provide a clean, testable way to access the current user identifier
    // - Decouple application logic from ASP.NET Core (HttpContext, ClaimsPrincipal)
    // - Enable reuse in non-HTTP contexts (background jobs, message handlers, tests)
    //
    // Why generic (T : struct):
    // - Allows flexibility in identifier type (int, Guid, long, etc.)
    // - Enforces value-type identifiers (no reference types)
    // - Makes the abstraction reusable across different systems
    public interface IUserContext<T> where T : struct
    {
        // The current authenticated user's identifier
        //
        // Nullable because:
        // - User may not be authenticated
        // - Context may be executed outside a request scope
        //
        // Read-only by design:
        // - User identity should not be mutable at runtime
        public T? userId { get; }
    }

    // Non-generic shortcut interface
    //
    // Purpose:
    // - Convenience alias for the most common identifier type
    // - Reduces verbosity in constructors and service registrations
    //
    // Example usage:
    // - public MyService(IUserContext userContext)
    //
    // This pattern allows:
    // - Easy refactoring if identifier type changes later
    // - Strong architectural boundary between security and business logic
    public interface IUserContext : IUserContext<int>
    {
    }
}
