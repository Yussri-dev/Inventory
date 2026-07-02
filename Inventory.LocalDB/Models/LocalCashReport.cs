using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalCashReport
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ServerId { get; set; }

        [Required]
        public Guid LocalCashSessionId { get; set; }

        public LocalCashSession LocalCashSession { get; set; } = null!;

        public Guid? ServerCashSessionId { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; } = string.Empty;

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

        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

        public Guid? GeneratedByUserId { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [Required, MaxLength(50)]
        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;

        public DateTime? LastSyncedAtUtc { get; set; }
    }
}