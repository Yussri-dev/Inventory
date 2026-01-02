using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.LoyaltyCards.Requests
{
    public class UpdateLoyaltyCardRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string CardNumber { get; set; } = null!;

        [Required]
        public Guid CustomerId { get; set; }

        public int CurrentPoints { get; set; }
        public int LifetimePoints { get; set; }

        public bool IsActive { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime IssuedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
