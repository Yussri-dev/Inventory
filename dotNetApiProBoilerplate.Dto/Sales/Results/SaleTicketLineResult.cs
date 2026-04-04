namespace Inventory.Dto.Sales.Results
{
    public class SaleTicketLineResult
    {
        public string ProductName { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }
    public class TicketPaymentLine
    {
        public string Method { get; set; } = "";   // "Cash", "Card", "Credit"
        public decimal Amount { get; set; }

        public string Label => Method switch
        {
            "Cash" => "Espèces",
            "Card" => "Carte bancaire",
            "Credit" => "Crédit client",
            _ => Method
        };
    }
}
