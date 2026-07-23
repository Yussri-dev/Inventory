namespace Inventory.LocalDB.Services.Results;

public sealed class LocalDamageDraftResult
{
    public Guid Id { get; set; }

    public string DamageNumber { get; set; } =
        string.Empty;

    public Guid ProductLocalId { get; set; }

    public Guid? ProductServerId { get; set; }

    public string ProductName { get; set; } =
        string.Empty;

    public decimal Quantity { get; set; }

    public decimal EstimatedValue { get; set; }

    public string? Reason { get; set; }

    public DateTime DamageDateUtc { get; set; }
}