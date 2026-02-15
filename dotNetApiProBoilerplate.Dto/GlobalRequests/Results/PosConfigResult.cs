namespace Inventory.Dto.GlobalRequests.Results
{
    public sealed class PosConfigResult
    {
        public string Currency { get; set; } = "EUR";
        public decimal DefaultVatRate { get; set; }
        public bool AllowNegativeStock { get; set; }
    }

}
