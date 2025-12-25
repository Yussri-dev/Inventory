using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.CashReports.Requests
{
    public class CreateCashReportRequest
    {
        [Required]
        public Guid CashSessionId { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CountedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Difference { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashSales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CardSales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherPayments { get; set; }

        public int TotalTransactions { get; set; }

        public DateTime GeneratedAt { get; set; }

        [Required]
        public Guid GeneratedByUserId { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
