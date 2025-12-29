using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.ReturnLines.Results
{
    public class ReturnLineResult
    {
        public Guid Id { get; set; }

        public Guid ReturnId { get; set; }

        public Guid ProductId { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal VatRate { get; set; }

        public decimal LineAmount => Quantity * UnitPrice;

        public string? Reason { get; set; }

        public bool RestockItem { get; set; }
    }
}
