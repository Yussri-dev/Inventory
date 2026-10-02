namespace Inventory.Dto.Sales.Results;

public sealed class SaleCustomerSnapshot
{
    public Guid SaleId { get; set; }
    public Guid? CustomerId { get; set; }
}

public sealed class SaleCustomerSyncResult
{
    public List<SaleCustomerSnapshot> Sales { get; set; } = new();
    public List<CorrectedCustomerBalance> Customers { get; set; } = new();
    public List<CorrectionLedgerEntry> Entries { get; set; } = new();
}

public sealed class CorrectedCustomerBalance
{
    public Guid CustomerId { get; set; }
    public decimal Balance { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public bool AllowCredit { get; set; }
    public bool HasUnlimitedCredit { get; set; }
    public decimal CreditLimit { get; set; }
}

public sealed class CorrectionLedgerEntry
{
    public Guid Id { get; set; }
    public Guid OperationId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid SaleId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
}
