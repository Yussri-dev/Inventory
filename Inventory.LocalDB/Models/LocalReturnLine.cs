using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalReturnLine
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid LocalReturnId { get; set; }

        public LocalReturn LocalReturn { get; set; } = null!;

        [Required]
        public Guid ProductLocalId { get; set; }

        public Guid? ProductServerId { get; set; }

        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ProductBarcode { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [NotMapped]
        public decimal LineAmount => Quantity * UnitPrice;

        [MaxLength(500)]
        public string? Reason { get; set; }

        public bool RestockItem { get; set; }
    }
}