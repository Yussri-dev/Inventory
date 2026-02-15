namespace Inventory.Dto.Sales.Results
{
    public class SaleTicketLineResult
    {
        public string ProductName { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }
}
