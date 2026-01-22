using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.CashMovements.Requests
{
    public class CreateCashMovementRequest
    {
        [Required]
        public Guid CashSessionId { get; set; }

        [Required]
        public CashMovementType Type { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string? Reason { get; set; }
        public Guid? SaleId { get; set; }
    }
}
