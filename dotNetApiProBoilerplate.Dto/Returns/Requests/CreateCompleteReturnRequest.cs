using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Returns.Requests
{
    public sealed class CreateCompleteReturnRequest
    {
        
        [Required]
        public Guid ClientOperationId { get; set; }

        [Required]
        public Guid SaleId { get; set; }

       
        public Guid? CashSessionId { get; set; }

        public DateTime ReturnDate { get; set; } =
            DateTime.UtcNow;

        [Required]
        public RefundMethod RefundType { get; set; }

        [Required]
        [MinLength(
            1,
            ErrorMessage =
                "At least one return line is required.")]
        public List<ReturnLineItem> Lines { get; set; } =
            new();
    }
}
