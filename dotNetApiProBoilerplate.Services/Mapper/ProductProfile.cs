using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;

namespace Inventory.Services.Mapping
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateProductRequest, Product>()
    .ForMember(d => d.Id, o => o.Ignore())

    // Catalog-owned fields (must NOT be set here)
    .ForMember(d => d.Name, o => o.Ignore())
    .ForMember(d => d.Sku, o => o.Ignore())
    .ForMember(d => d.Barcode, o => o.Ignore())
    .ForMember(d => d.Brand, o => o.Ignore())
    .ForMember(d => d.Description, o => o.Ignore())
    .ForMember(d => d.Category, o => o.Ignore())
    .ForMember(d => d.Unit, o => o.Ignore())
    .ForMember(d => d.ImageUrl, o => o.Ignore())

    // Navigation / infra
    .ForMember(d => d.CatalogProduct, o => o.Ignore())
    .ForMember(d => d.Stock, o => o.Ignore())
    .ForMember(d => d.StockMovements, o => o.Ignore())
    .ForMember(d => d.SaleLines, o => o.Ignore())
    .ForMember(d => d.PurchaseLines, o => o.Ignore())

    // Audit
    .ForMember(d => d.CreatedAt, o => o.Ignore())
    .ForMember(d => d.ModifiedAt, o => o.Ignore())
    .ForMember(d => d.IsDeleted, o => o.Ignore())
    .ForMember(d => d.DeletedAt, o => o.Ignore())
    .ForMember(d => d.TenantId, o => o.Ignore())
    .ForMember(d => d.CreatedByUserId, o => o.Ignore())
    .ForMember(d => d.ModifiedByUserId, o => o.Ignore())
    .ForMember(d => d.DeletedByUserId, o => o.Ignore())

    // ✅ Tenant-owned fields (CORRECT)
    .ForMember(d => d.CatalogProductId, o => o.MapFrom(s => s.CatalogProductId))
    .ForMember(d => d.SalePrice, o => o.MapFrom(s => s.SalePrice))
    .ForMember(d => d.PurchasePrice, o => o.MapFrom(s => s.PurchasePrice))
    .ForMember(d => d.VatRate, o => o.MapFrom(s => s.VatRate))
    .ForMember(d => d.MinStockLevel, o => o.MapFrom(s => s.MinStockLevel))
    .ForMember(d => d.MaxStockLevel, o => o.MapFrom(s => s.MaxStockLevel))
    .ForMember(d => d.IsTracked, o => o.MapFrom(s => s.IsTracked))
    .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));


            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateProductRequest, Product>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.CatalogProductId, o => o.Ignore())
                .ForMember(d => d.CatalogProduct, o => o.Ignore())
                .ForMember(d => d.Name, o => o.Ignore())
                .ForMember(d => d.Sku, o => o.Ignore())
                .ForMember(d => d.Barcode, o => o.Ignore())
                .ForMember(d => d.Brand, o => o.Ignore())
                .ForMember(d => d.Description, o => o.Ignore())
                .ForMember(d => d.Category, o => o.Ignore())
                .ForMember(d => d.Unit, o => o.Ignore())
                .ForMember(d => d.ImageUrl, o => o.Ignore())
                .ForMember(d => d.Stock, o => o.Ignore())
                .ForMember(d => d.StockMovements, o => o.Ignore())
                .ForMember(d => d.SaleLines, o => o.Ignore())
                .ForMember(d => d.PurchaseLines, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.ModifiedAt, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore())
                .ForMember(d => d.DeletedAt, o => o.Ignore())
                .ForMember(d => d.TenantId, o => o.Ignore())
                .ForMember(d => d.CreatedByUserId, o => o.Ignore())
                .ForMember(d => d.ModifiedByUserId, o => o.Ignore())
                .ForMember(d => d.DeletedByUserId, o => o.Ignore())
                // Map only tenant-specific fields that can be updated
                .ForMember(d => d.SalePrice, o => o.MapFrom(s => s.SalePrice))
                .ForMember(d => d.PurchasePrice, o => o.MapFrom(s => s.PurchasePrice))
                .ForMember(d => d.VatRate, o => o.MapFrom(s => s.VatRate))
                .ForMember(d => d.MinStockLevel, o => o.MapFrom(s => s.MinStockLevel))
                .ForMember(d => d.MaxStockLevel, o => o.MapFrom(s => s.MaxStockLevel))
                .ForMember(d => d.IsTracked, o => o.MapFrom(s => s.IsTracked))
                .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));

            // =========================
            // RESULT - Map from Product Entity to ProductResult DTO
            // =========================
            CreateMap<Product, ProductResult>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.CatalogProductId, o => o.MapFrom(s => s.CatalogProductId))
                // Catalog fields (denormalized for display)
                .ForMember(d => d.CatalogName, o => o.MapFrom(s => s.CatalogProduct != null ? s.CatalogProduct.Name : s.Name))
                .ForMember(d => d.CatalogBrand, o => o.MapFrom(s => s.CatalogProduct != null ? s.CatalogProduct.Brand : s.Brand))
                .ForMember(d => d.CatalogBarcode, o => o.MapFrom(s => s.CatalogProduct != null ? s.CatalogProduct.Barcode : s.Barcode))
                // Tenant-specific fields
                .ForMember(d => d.SalePrice, o => o.MapFrom(s => s.SalePrice))
                .ForMember(d => d.PurchasePrice, o => o.MapFrom(s => s.PurchasePrice))
                .ForMember(d => d.VatRate, o => o.MapFrom(s => s.VatRate))
                .ForMember(d => d.MinStockLevel, o => o.MapFrom(s => s.MinStockLevel))
                .ForMember(d => d.MaxStockLevel, o => o.MapFrom(s => s.MaxStockLevel))
                .ForMember(d => d.IsTracked, o => o.MapFrom(s => s.IsTracked))
                .ForMember(d => d.Status, o => o.MapFrom(s => (Dto.Enums.ProductStatus)s.IsActive));
        }
    }
}