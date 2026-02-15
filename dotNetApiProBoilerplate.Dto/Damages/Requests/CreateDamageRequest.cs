using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Damages.Requests
{
    public class CreateDamageRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(0.001, double.MaxValue)]
        public decimal Quantity { get; set; }

        public decimal EstimatedValue { get; set; }

        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        public string? Photos { get; set; }
        public string? Notes { get; set; }
    }

}
