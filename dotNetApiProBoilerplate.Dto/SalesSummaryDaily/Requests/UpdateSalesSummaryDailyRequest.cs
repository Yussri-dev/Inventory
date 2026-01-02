

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.SalesSummaryDaily.Requests
{
    public class UpdateSalesSummaryDailyRequest
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime Date { get; set; }

        public int TotalTransactions { get; set; }
        public int TotalItems { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRevenue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVat { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDiscount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashSales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CardSales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditSales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AverageTransactionValue { get; set; }

        public DateTime GeneratedAt { get; set; }
    }
}
