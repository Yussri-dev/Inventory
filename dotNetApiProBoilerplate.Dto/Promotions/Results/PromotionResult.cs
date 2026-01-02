
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.Promotions.Results
{
    public class PromotionResult
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string? Description { get; set; }

        public string Type { get; set; } = null!; // Percentage, FixedAmount, BuyXGetY

        public decimal DiscountValue { get; set; }

        public decimal? MinimumPurchaseAmount { get; set; }

        public decimal? MaximumDiscountAmount { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public int? MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }

        public int? MaxUsagePerCustomer { get; set; }

        public string? ApplicableToCategory { get; set; }

        public Guid? ApplicableToProductId { get; set; }

        public bool CombinableWithOtherPromotions { get; set; }

    }
}
