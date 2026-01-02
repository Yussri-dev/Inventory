
using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.LoyaltyTransactions.Requests
{
    public class UpdateLoyaltyTransactionRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid LoyaltyCardId { get; set; }

        public Guid? SaleId { get; set; }

        public int PointsChange { get; set; }
        public int PointsBefore { get; set; }
        public int PointsAfter { get; set; }

        [Required, MaxLength(200)]
        public string Reason { get; set; } = null!;

        public DateTime TransactionDate { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
