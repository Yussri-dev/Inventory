namespace Inventory.LocalDB.Models
{
    public interface ILocalTenantEntity
    {
        Guid TenantId { get; set; }
    }
}
