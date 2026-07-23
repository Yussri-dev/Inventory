using System.ComponentModel.DataAnnotations;


namespace Inventory.LocalDB.Services.Requests
{
    public sealed class CreateLocalPurchaseRequest
    {
        [Required]
        public Guid SupplierLocalId { get; set; }

        [MaxLength(100)]
        public string? SupplierInvoiceNumber { get; set; }

        public DateTime PurchaseDateUtc { get; set; } =
            DateTime.UtcNow;

        public DateTime? ExpectedDeliveryDateUtc { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreateLocalPurchaseLineRequest> Lines { get; set; } =
            new();
    }
}
