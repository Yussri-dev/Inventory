
using Inventory.Domain.Abstraction;
using Inventory.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class CashReport : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CashSessionId { get; set; }

        [ForeignKey(nameof(CashSessionId))]
        public CashSession CashSession { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Type { get; set; } = null!; // Hourly, Daily, Shift

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

        [ForeignKey(nameof(GeneratedByUserId))]
        public ApplicationUser GeneratedByUser { get; set; } = null!;

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
