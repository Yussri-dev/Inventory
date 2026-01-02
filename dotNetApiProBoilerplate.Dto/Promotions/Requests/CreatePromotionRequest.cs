
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.Promotions.Requests
{
    public class CreatePromotionRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Code { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; } = null!; // Percentage, FixedAmount, BuyXGetY

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumPurchaseAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumDiscountAmount { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public int? MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }

        public int? MaxUsagePerCustomer { get; set; }

        [MaxLength(100)]
        public string? ApplicableToCategory { get; set; }

        public Guid? ApplicableToProductId { get; set; }

        public bool CombinableWithOtherPromotions { get; set; }

    }
}
