namespace Inventory.Dto.Sales.Results
{
    public sealed class CreateCompleteSaleResult
    {
        public Guid Id { get; set; }

        public SaleResult Sale { get; init; }
        public SaleTicketResult Ticket { get; init; }
        public string PdfBase64 { get; init; }

    }

}
