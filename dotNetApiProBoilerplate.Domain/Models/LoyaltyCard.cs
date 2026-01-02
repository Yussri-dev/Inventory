
using Inventory.Domain.Abstraction;
using Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Models
{
    // ===============================
    // 10. LOYALTY & RECEIPTS
    // ===============================

    public class LoyaltyCard : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string CardNumber { get; set; } = null!;

        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        public int CurrentPoints { get; set; }
        public int LifetimePoints { get; set; }

        public bool IsActive { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime IssuedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
    }

}
