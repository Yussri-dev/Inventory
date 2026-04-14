namespace Inventory.Services.Abstractions
{
    public interface IPackService
    {
        bool IsPack(Guid catalogProductId);
        Guid? GetComponentCatalogId(Guid catalogProductId);
        decimal GetPackSize(Guid catalogProductId);
        decimal GetUnitQuantity(Guid catalogProductId, decimal quantity);
        void InvalidateCache(Guid catalogProductId);
    }
}
