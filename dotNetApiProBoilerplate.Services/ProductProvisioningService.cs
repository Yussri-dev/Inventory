using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Data;
using Inventory.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Services
{
    public sealed class ProductProvisioningService
       : IProductProvisioningService
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<ProductProvisioningService> _logger;

        public ProductProvisioningService(
            InventoryDbContext context,
            ILogger<ProductProvisioningService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> ProvisionCatalogProductsAsync(
            Guid tenantId,
            Guid createdByUserId,
            CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty)
            {
                throw new ArgumentException(
                    "TenantId cannot be empty.",
                    nameof(tenantId));
            }

            if (createdByUserId == Guid.Empty)
            {
                throw new ArgumentException(
                    "CreatedByUserId cannot be empty.",
                    nameof(createdByUserId));
            }

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(
                    x => x.Id == tenantId,
                    cancellationToken);

            if (tenant is null)
            {
                throw new InvalidOperationException(
                    $"Tenant '{tenantId}' does not exist.");
            }

            var catalogProducts = await _context.ProductCatalogs
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "{CatalogCount} active catalog products found",
                catalogProducts.Count);

            if (catalogProducts.Count == 0)
            {
                _logger.LogWarning(
                    "No active ProductCatalog found. No Product will be created.");

                return 0;
            }

            var existingCatalogIds = await _context.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    !x.IsDeleted)
                .Select(x => x.CatalogProductId)
                .ToHashSetAsync(cancellationToken);

            _logger.LogInformation(
                "{ExistingCount} existing tenant products found",
                existingCatalogIds.Count);

            var now = DateTime.UtcNow;

            var products = catalogProducts
                .Where(x => !existingCatalogIds.Contains(x.Id))
                .Select(x => new Product
                {
                    Id = Guid.NewGuid(),
                    CatalogProductId = x.Id,

                    Name = x.Name.Trim(),
                    Sku = ResolveSku(x),

                    Barcode = NormalizeNullable(x.Barcode),
                    Brand = NormalizeNullable(x.Brand),
                    Description = NormalizeNullable(x.Description),

                    Category = null,

                    SalePrice = 0m,
                    SalePrice2 = 0m,
                    SalePrice3 = 0m,
                    PurchasePrice = 0m,

                    VatRate = 0m,
                    MinStockLevel = 0m,
                    MaxStockLevel = 0m,

                    Unit = string.IsNullOrWhiteSpace(x.UnitOfMeasure)
                        ? "pcs"
                        : x.UnitOfMeasure.Trim(),

                    IsActive = ProductStatus.Active,
                    IsTracked = true,

                    ImageUrl = null,

                    TenantId = tenantId,

                    CreatedAt = now,
                    CreatedByUserId = createdByUserId,

                    ModifiedAt = null,
                    ModifiedByUserId = null,

                    IsDeleted = false,
                    DeletedAt = null,
                    DeletedByUserId = null
                })
                .ToList();

            _logger.LogInformation(
                "{ProductCount} products prepared for insertion",
                products.Count);

            if (products.Count == 0)
                return 0;

            //if (tenant.CurrentProducts + products.Count > tenant.MaxProducts)
            //{
            //    throw new InvalidOperationException(
            //        $"The global catalog contains {products.Count} new products, " +
            //        $"but tenant '{tenant.Name}' can only contain " +
            //        $"{tenant.MaxProducts} products.");
            //}

            await _context.Products.AddRangeAsync(
                products,
                cancellationToken);

            tenant.CurrentProducts += products.Count;

            try
            {
                var affectedRows =
                    await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Product provisioning saved successfully. " +
                    "Affected rows: {AffectedRows}",
                    affectedRows);
            }
            catch (DbUpdateException exception)
            {
                var databaseError =
                    exception.InnerException?.Message ??
                    exception.Message;

                _logger.LogError(
                    exception,
                    "Product provisioning database error: {DatabaseError}",
                    databaseError);

                throw new InvalidOperationException(
                    $"Product provisioning failed: {databaseError}",
                    exception);
            }

            return products.Count;
        }

        public async Task<int> ProvisionCatalogProductToAllTenantsAsync(
    Guid catalogProductId,
    Guid createdByUserId,
    CancellationToken cancellationToken = default)
        {
            if (catalogProductId == Guid.Empty)
                throw new ArgumentException("Catalog product id is required.", nameof(catalogProductId));

            if (createdByUserId == Guid.Empty)
                throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));

            var catalog = await _context.ProductCatalogs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == catalogProductId && !x.IsDeleted,
                    cancellationToken);

            if (catalog is null)
                throw new InvalidOperationException("Catalog product not found.");

            var tenants = await _context.Tenants
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            if (tenants.Count == 0)
                return 0;

            var tenantIds = tenants
                .Select(x => x.Id)
                .ToList();

            var existingTenantIds = await _context.Products
                .AsNoTracking()
                .Where(x =>
                    tenantIds.Contains(x.TenantId) &&
                    x.CatalogProductId == catalogProductId &&
                    !x.IsDeleted)
                .Select(x => x.TenantId)
                .ToListAsync(cancellationToken);

            var existingTenantIdSet = existingTenantIds.ToHashSet();

            var now = DateTime.UtcNow;

            var products = tenants
                .Where(tenant => !existingTenantIdSet.Contains(tenant.Id))
                .Select(tenant => new Product
                {
                    Id = Guid.NewGuid(),

                    CatalogProductId = catalog.Id,

                    Name = catalog.Name.Trim(),
                    Sku = ResolveSku(catalog),
                    Barcode = NormalizeNullable(catalog.Barcode),
                    Brand = NormalizeNullable(catalog.Brand),
                    Description = NormalizeNullable(catalog.Description),

                    Category = null,

                    SalePrice = 0m,
                    SalePrice2 = 0m,
                    SalePrice3 = 0m,
                    PurchasePrice = 0m,
                    VatRate = 0m,

                    MinStockLevel = 0m,
                    MaxStockLevel = 0m,

                    Unit = string.IsNullOrWhiteSpace(catalog.UnitOfMeasure)
                        ? "pcs"
                        : catalog.UnitOfMeasure.Trim(),

                    IsActive = ProductStatus.Active,
                    IsTracked = true,

                    ImageUrl = null,

                    TenantId = tenant.Id,

                    CreatedAt = now,
                    CreatedByUserId = createdByUserId,

                    ModifiedAt = null,
                    ModifiedByUserId = null,

                    IsDeleted = false,
                    DeletedAt = null,
                    DeletedByUserId = null
                })
                .ToList();

            if (products.Count == 0)
                return 0;

            await _context.Products.AddRangeAsync(products, cancellationToken);

            foreach (var tenant in tenants)
            {
                if (!existingTenantIdSet.Contains(tenant.Id))
                    tenant.CurrentProducts += 1;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return products.Count;
        }
        private static string ResolveSku(ProductCatalog catalog)
        {
            if (!string.IsNullOrWhiteSpace(catalog.InternalCode))
                return catalog.InternalCode.Trim();

            if (!string.IsNullOrWhiteSpace(catalog.Barcode))
                return catalog.Barcode.Trim();

            return $"CAT-{catalog.Id:N}";
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}