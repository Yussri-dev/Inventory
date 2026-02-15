namespace Inventory.Dto.SaleLines.Results
{
    public class SaleLineWithReturnsResult
    {
        public Guid Id { get; set; }
        public Guid SaleId { get; set; }
        public Guid ProductId { get; set; }

        public decimal Quantity { get; set; }           // vendu
        public decimal ReturnedQuantity { get; set; }   // déjà retourné
        public decimal AvailableQuantity { get; set; }  // retournable

        public decimal UnitPrice { get; set; }
        public decimal VatRate { get; set; }

        public decimal LineAmountInclVat { get; set; }
    }


}
