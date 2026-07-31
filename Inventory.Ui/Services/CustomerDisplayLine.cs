namespace Inventory.Ui.Services
{
    public sealed record CustomerDisplayLine(
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

    public sealed record CustomerDisplaySnapshot(
        IReadOnlyList<CustomerDisplayLine> Lines,
        decimal Subtotal,
        decimal Vat,
        decimal Discount,
        decimal Total,
        string? LastScannedProduct,
        CustomerDisplayMode Mode);

    public enum CustomerDisplayMode
    {
        Idle,
        Sale,
        Payment,
        Completed
    }
}
