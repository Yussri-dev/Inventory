using Inventory.Dto.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.CashMovements.Requests
{
    public class CreateCashMovementRequest
    {
        [Required]
        public Guid CashSessionId { get; set; }

        [Required]
        public CashMovementType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceBefore { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public Guid? SaleId { get; set; }

        public DateTime MovementDate { get; set; }
    }
}
