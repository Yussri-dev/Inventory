
using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.Sales.Results
{
    public class SaleResult
    {
        public Guid Id { get; set; }

        public string InvoiceNumber { get; set; } = null!;

        public decimal SubtotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal VatAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal ChangeAmount { get; set; }

        public SaleStatus Status { get; set; }

        public PaymentStatus PaymentStatus { get; set; }

        public Guid? CustomerId { get; set; }

        public Guid CashSessionId { get; set; }

        public DateTime SaleDate { get; set; }

        public string? Notes { get; set; }
    }
}
