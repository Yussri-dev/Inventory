
using Inventory.Domain.Abstraction;
using Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Models
{
    public class LoyaltyTransaction : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid LoyaltyCardId { get; set; }

        [ForeignKey(nameof(LoyaltyCardId))]
        public LoyaltyCard LoyaltyCard { get; set; } = null!;

        public Guid? SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public Sale? Sale { get; set; }

        public int PointsChange { get; set; }
        public int PointsBefore { get; set; }
        public int PointsAfter { get; set; }

        [Required, MaxLength(200)]
        public string Reason { get; set; } = null!;

        public DateTime TransactionDate { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }

}
