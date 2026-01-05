using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Inventory.Domain.Abstraction;
using Inventory.Domain.Enums;
using Inventory.Domain.Models;

namespace Inventory.Domain.Entities
{
    public class SupplierTransaction : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public SupplierTransactionType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime TransactionDate { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; } // Invoice # or Payment Ref

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Optional links
        public Guid? PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }

        public Guid? SupplierReturnId { get; set; }
        public SupplierReturn? SupplierReturn { get; set; }
    }
}
