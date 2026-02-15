namespace Inventory.Dto.Sales.Results
{
    public class SaleTicketResult
    {
        public string InvoiceNumber { get; set; } = null!;
        public DateTime SaleDate { get; set; }

        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string StoreAddress { get; set; } = null!;

        public List<SaleTicketLineResult> Lines { get; set; } = new();

        public decimal Subtotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }

        public decimal Paid { get; set; }
        public decimal Change { get; set; }
    }

}
