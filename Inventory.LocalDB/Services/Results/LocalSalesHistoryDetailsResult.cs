using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalSalesHistoryDetailsResult
    {
        public Guid LocalId { get; set; }

        public string InvoiceNumber { get; set; } =
            string.Empty;

        public DateTime SaleDateUtc { get; set; }

        public string? CustomerName { get; set; }

        public decimal SubtotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal VatAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal ChangeAmount { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string PaymentStatus { get; set; } =
            string.Empty;

        public string SyncStatus { get; set; } =
            string.Empty;

        public List<LocalSalesHistoryLineResult> Lines { get; set; } =
            new();

        public List<LocalSalesHistoryPaymentResult> Payments { get; set; } =
            new();
    }
}
