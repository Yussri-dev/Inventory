using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Auth.Results
{
    public class AuthResult
    {
        // JWT court
        public string AccessToken { get; set; } = null!;

        // Token long (refresh)
        public string RefreshToken { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }

        // Infos utilisateur
        public Guid UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string Role { get; set; } = null!;
        public DateTime? TrialEndDate { get; set; }
        public bool IsTrialActive { get; set; }
    }
}
