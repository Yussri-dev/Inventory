using Inventory.Domain.Abstraction;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class SaleLine : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public Sale Sale { get; set; } = null!;

        // =========================
        // WHAT USER SOLD (UI / BUSINESS)
        // =========================
        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        // =========================
        // WHAT HITS STOCK (INVENTORY)
        // =========================
        public Guid UnitProductId { get; set; }

        [ForeignKey(nameof(UnitProductId))]
        public Product UnitProduct { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        public decimal UnitQuantity { get; set; }

        // =========================
        // FINANCIALS (TTC MODEL)
        // =========================
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } // TTC

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCostPrice { get; set; }
        // =========================
        // CALCULATED (READ ONLY)
        // =========================
        [NotMapped]
        public decimal LineTTC => (UnitPrice * Quantity) - DiscountAmount;

        [NotMapped]
        public decimal LineHT
        {
            get
            {
                var divisor = 1 + (VatRate / 100m);
                return divisor == 0 ? LineTTC : LineTTC / divisor;
            }
        }

        [NotMapped]
        public decimal VatAmount => LineTTC - LineHT;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
