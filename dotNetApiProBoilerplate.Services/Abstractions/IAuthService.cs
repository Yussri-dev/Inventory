// Base .NET namespaces
// Included for consistency with other service abstraction files
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Abstractions
{
    // Authentication service contract
    //
    // Purpose:
    // - Defines the public API of the authentication domain
    // - Acts as a boundary between:
    //   - API layer (controllers)
    //   - Infrastructure (Identity, JWT, EF Core)
    //
    // Why this interface exists even if empty:
    // - Establishes a clean architecture rule: depend on abstractions
    // - Allows swapping implementations without touching controllers
    // - Makes the boilerplate extensible and testable
    //
    // Why it is empty in a boilerplate:
    // - Avoids enforcing premature design decisions
    // - Leaves freedom to the consumer to shape auth flows
    // - Keeps focus on structure, not feature completeness
    //
    // Typical responsibilities (documented, not implemented here):
    // - User registration
    // - User login
    // - Token refresh
    // - Password change
    // - Account lifecycle management
    //
    // Typical future shape:
    //
    // Task<AuthResult> RegisterAsync(RegisterRequest request);
    // Task<AuthResult> LoginAsync(LoginRequest request);
    // Task<AuthResult> RefreshTokenAsync(string userId);
    // Task ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    //
    // Architectural rule this enables:
    // - Controllers depend on IAuthService
    // - Concrete AuthService lives in Services layer
    // - Infrastructure concerns stay isolated
    public interface IAuthService
    {
    }
}
