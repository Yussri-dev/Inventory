using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.CashMovements.Results
{
    public class CashMovementResult
    {
        public Guid Id { get; set; }

        public Guid CashSessionId { get; set; }

        public CashMovementType Type { get; set; }

        public decimal Amount { get; set; }

        public decimal BalanceBefore { get; set; }

        public decimal BalanceAfter { get; set; }

        public string? Reason { get; set; }

        public Guid? SaleId { get; set; }

        public DateTime MovementDate { get; set; }
    }
}
