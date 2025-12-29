
using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.StockMouvements.Requests
{
    public class CreateStockMouvementRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityChange { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityBefore { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityAfter { get; set; }

        [Required]
        public StockMovementType Type { get; set; }

        public Guid? ReferenceId { get; set; } // ID de la vente, achat, etc.

        [MaxLength(200)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime MovementDate { get; set; }
    }
}
