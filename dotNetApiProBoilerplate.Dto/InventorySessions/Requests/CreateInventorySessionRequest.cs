using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.InventorySessions.Requests
{
    public class CreateInventorySessionRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string SessionNumber { get; set; } = null!;

        [MaxLength(200)]
        public string? Name { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? ValidatedAt { get; set; }

        [Required]
        public InventoryStatus Status { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Guid? ValidatedByUserId { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public int TotalProductsCounted { get; set; }
        public int TotalDiscrepancies { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVarianceValue { get; set; }
    }
}
