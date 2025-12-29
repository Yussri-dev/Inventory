using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.Returns.Requests
{
    public class UpdateReturnRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string ReturnNumber { get; set; } = null!;

        [Required]
        public Guid SaleId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(1000)]
        public string? Reason { get; set; }

        public DateTime ReturnDate { get; set; }

        public bool IsProcessed { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
