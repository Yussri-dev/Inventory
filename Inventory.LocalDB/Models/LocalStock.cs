using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalStock
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ServerId { get; set; }

        [Required]
        public Guid ProductLocalId { get; set; }

        public Guid ProductServerId { get; set; }

        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ProductBarcode { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal ReservedQuantity { get; set; }

        [NotMapped]
        public decimal AvailableQuantity => Quantity - ReservedQuantity;

        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastSyncedAtUtc { get; set; }
    }
}
