namespace Inventory.Dto.Analytics.Results
{
    public class RecentSaleResult
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string CustomerName { get; set; } = "Walk-in Customer";
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentSummary { get; set; } = string.Empty;
    }
}
