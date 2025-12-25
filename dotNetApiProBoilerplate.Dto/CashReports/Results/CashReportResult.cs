
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.CashReports.Results
{
    public class CashReportResult
    {
        public Guid Id { get; set; }

        public Guid CashSessionId { get; set; }

        public string Type { get; set; } = null!;

        public decimal ExpectedAmount { get; set; }

        public decimal? CountedAmount { get; set; }

        public decimal Difference { get; set; }

        public decimal CashSales { get; set; }

        public decimal CardSales { get; set; }

        public decimal OtherPayments { get; set; }

        public int TotalTransactions { get; set; }

        public DateTime GeneratedAt { get; set; }

        public Guid GeneratedByUserId { get; set; }

        public string? Notes { get; set; }
    }
}
