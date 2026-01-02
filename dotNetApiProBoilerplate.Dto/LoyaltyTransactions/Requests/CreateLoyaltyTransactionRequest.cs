
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.LoyaltyTransactions.Requests
{
    public class CreateLoyaltyTransactionRequest
    {
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
