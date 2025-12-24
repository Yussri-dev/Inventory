namespace Inventory.Dto.CashCorrections.Results
{
    public class CashCorrectionResult
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? TaxNumber { get; set; }

        public decimal CreditLimit { get; set; }

        public decimal CurrentBalance { get; set; }

        public bool IsActive { get; set; }

        public string? Notes { get; set; }
    }
}
