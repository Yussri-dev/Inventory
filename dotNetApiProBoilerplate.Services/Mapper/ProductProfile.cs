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
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Stock, opt => opt.Ignore())
                .ForMember(dest => dest.StockMovements, opt => opt.Ignore())
                .ForMember(dest => dest.SaleLines, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseLines, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateProductRequest, Product>()
                // Identité & audit
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())

                // Navigation / collections
                .ForMember(dest => dest.Stock, opt => opt.Ignore())
                .ForMember(dest => dest.StockMovements, opt => opt.Ignore())
                .ForMember(dest => dest.SaleLines, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseLines, opt => opt.Ignore())

                // Strings — mise à jour contrôlée
                .ForMember(dest => dest.Name,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Name)))
                .ForMember(dest => dest.Sku,
                    opt => opt.Condition(src => src.Sku != null))
                .ForMember(dest => dest.Barcode,
                    opt => opt.Condition(src => src.Barcode != null))
                .ForMember(dest => dest.Description,
                    opt => opt.Condition(src => src.Description != null))
                .ForMember(dest => dest.Category,
                    opt => opt.Condition(src => src.Category != null))
                .ForMember(dest => dest.Brand,
                    opt => opt.Condition(src => src.Brand != null))
                .ForMember(dest => dest.Unit,
                    opt => opt.Condition(src => src.Unit != null))
                .ForMember(dest => dest.ImageUrl,
                    opt => opt.Condition(src => src.ImageUrl != null))

                // Décimaux — toujours mappés (déjà validés dans le DTO)
                .ForMember(dest => dest.SalePrice, opt => opt.MapFrom(src => src.SalePrice))
                .ForMember(dest => dest.PurchasePrice, opt => opt.MapFrom(src => src.PurchasePrice))
                .ForMember(dest => dest.VatRate, opt => opt.MapFrom(src => src.VatRate))
                .ForMember(dest => dest.MinStockLevel, opt => opt.MapFrom(src => src.MinStockLevel))
                .ForMember(dest => dest.MaxStockLevel, opt => opt.MapFrom(src => src.MaxStockLevel))

                // Flags / enums
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsTracked, opt => opt.MapFrom(src => src.IsTracked));

            // =========================
            // RESULT
            // =========================
            CreateMap<Product, ProductResult>()
                .ForMember(
                    d => d.Status,
                    opt => opt.MapFrom(s => (Dto.Enums.ProductStatus)s.IsActive)
                );
        }
    }
}
