namespace Inventory.Services.Abstractions
{
    public interface IProductProvisioningService
    {
        Task<int> ProvisionCatalogProductsAsync(
            Guid tenantId,
            Guid createdByUserId,
            CancellationToken cancellationToken = default);

        Task<int> ProvisionCatalogProductToAllTenantsAsync(
             Guid catalogProductId,
             Guid createdByUserId,
             Guid? excludedTenantId = null,
             CancellationToken cancellationToken = default);
    }

}
