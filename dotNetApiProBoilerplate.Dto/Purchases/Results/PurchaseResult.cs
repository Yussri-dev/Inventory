using Inventory.Dto.Enums;

namespace Inventory.Dto.Purchases.Results
{
    public class PurchaseResult
    {
        public Guid Id { get; set; }

        public Guid SupplierId { get; set; }

        public string PurchaseNumber { get; set; } = null!;

        public string? SupplierInvoiceNumber { get; set; }

        public decimal TotalAmountExclVat { get; set; }

        public decimal TotalVatAmount { get; set; }

        public decimal TotalAmountInclVat { get; set; }

        public PurchaseStatus Status { get; set; }

        public DateTime PurchaseDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime? PaymentDueDate { get; set; }
        public DateTime? PaymentDate { get; set; }

        public string? Notes { get; set; }
    }
}
