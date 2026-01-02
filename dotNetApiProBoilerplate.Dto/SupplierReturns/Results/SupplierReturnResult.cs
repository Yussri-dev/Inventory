using Inventory.Dto.Enums;

namespace Inventory.Dto.SupplierReturns.Results
{
    public class SupplierReturnResult
    {
        public Guid Id { get; set; }

        public string ReturnNumber { get; set; } = null!;

        public Guid SupplierId { get; set; }

        public string Reason { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public SupplierReturnStatus Status { get; set; }

        public DateTime ReturnDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public Guid? PurchaseId { get; set; }

        public string? Notes { get; set; }
    }
}
