namespace Inventory.Dto.Customers.Results
{
    public class CustomerResult
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? TaxNumber { get; set; }

        public decimal CreditLimit { get; set; }

        public bool AllowCredit { get; set; }

        public bool HasUnlimitedCredit { get; set; }

        public decimal CurrentBalance { get; set; }

        public bool IsActive { get; set; }

        public string? Notes { get; set; }
    }
}
