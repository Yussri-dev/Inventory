using Inventory.Dto.Enums;
namespace Inventory.Dto.Receipts
{
    public class ReceiptModel
    {
        public string SaleNumber { get; set; } = "";
        public DateTime Date { get; set; }
        public string? Customer { get; set; }
        public List<ReceiptLine> Lines { get; set; } = new();
        public decimal Total { get; set; }
        public decimal Tax { get; set; }
        public List<ReceiptPayment> Payments { get; set; } = new(); // Changed from PaymentMethod
    }

    public class ReceiptLine
    {
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class ReceiptPayment
    {
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
    }
}