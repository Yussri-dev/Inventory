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
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public Guid TenantId { get; set; }
        public string? FullName { get; set; }
        public string Role { get; set; } = null!;
    }
}
