namespace Inventory.Dto.Sales.Results
{
    public sealed class CreateCompleteSaleResult
    {
        public SaleResult Sale { get; init; }
        public SaleTicketResult Ticket { get; init; }
        public string PdfBase64 { get; init; }

    }

}
