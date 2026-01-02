
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.LoyaltyCards.Results
{
    public class LoyaltyCardResult
    {
        public Guid Id { get; set; }

        public string CardNumber { get; set; } = null!;

        public Guid CustomerId { get; set; }

        public int CurrentPoints { get; set; }
        public int LifetimePoints { get; set; }

        public bool IsActive { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime IssuedAt { get; set; }

        public string? Notes { get; set; }
    }
}
