
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.SaleReceipts.Requests
{
    public class CreateSaleReceiptRequest
    {
        [Required]
        public Guid SaleId { get; set; }

        [Required, MaxLength(100)]
        public string ReceiptNumber { get; set; } = null!;

        [Required]
        public string ReceiptData { get; set; } = null!; // JSON ou HTML

        [MaxLength(50)]
        public string Format { get; set; } = "JSON"; // JSON, HTML, XML

        public DateTime GeneratedAt { get; set; }

        public bool IsPrinted { get; set; }
        public DateTime? PrintedAt { get; set; }

        public bool IsEmailed { get; set; }
        public DateTime? EmailedAt { get; set; }

        [MaxLength(100)]
        public string? EmailAddress { get; set; }
    }
}
