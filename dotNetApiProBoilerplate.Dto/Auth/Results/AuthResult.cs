// Base .NET namespaces
// Included for consistency across DTO files
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Auth.Results
{
    // DTO returned after successful authentication
    // Used for login, registration, and token refresh responses
    public class AuthResult
    {
        // JWT access token issued by the authentication system
        // This token must be sent in the Authorization header for protected endpoints
        public string Token { get; init; } = null!;

        // Expiration date and time of the JWT token
        // Clients should refresh or re-authenticate before this moment
        public DateTime ExpiresAt { get; init; }
    }
}
