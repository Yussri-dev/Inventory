using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Returns.Requests
{
    public class CreateReturnRequest
    {
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
