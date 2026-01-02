
namespace Inventory.Dto.LoyaltyTransactions.Results
{
    public class LoyaltyTransactionResult
    {
        public Guid Id { get; set; }

        public Guid LoyaltyCardId { get; set; }

        public Guid? SaleId { get; set; }

        public int PointsChange { get; set; }
        public int PointsBefore { get; set; }
        public int PointsAfter { get; set; }

        public string Reason { get; set; } = null!;

        public DateTime TransactionDate { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
