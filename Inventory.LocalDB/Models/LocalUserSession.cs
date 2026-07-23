using System.ComponentModel.DataAnnotations;

namespace Inventory.LocalDB.Models
{
    public class LocalUserSession
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        public Guid? TenantId { get; set; }

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? FullName { get; set; }

        [Required, MaxLength(100)]
        public string Role { get; set; } = string.Empty;

        public DateTime TokenExpiresAtUtc { get; set; }

        public DateTime? TrialEndDateUtc { get; set; }

        public bool IsTrialActive { get; set; }

        public DateTime LastOnlineLoginAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastOfflineAccessAtUtc { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? OfflinePasswordHash { get; set; }

        [MaxLength(500)]
        public string? OfflinePasswordSalt { get; set; }

        public int OfflinePasswordIterations { get; set; } = 100_000;
    }
}