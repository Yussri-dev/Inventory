using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Dto.ProductCatalogs.Results;

namespace Inventory.Services.Mapping
{
    public class ProductCatalogProfile : Profile
    {
        public ProductCatalogProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateProductCatalogRequest, ProductCatalog>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.TenantProducts, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateProductCatalogRequest, ProductCatalog>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.TenantProducts, opt => opt.Ignore())

                // Strings — update contrôlé
                .ForMember(dest => dest.Barcode,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Barcode)))
                .ForMember(dest => dest.Name,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Name)))
                .ForMember(dest => dest.Brand,
                    opt => opt.Condition(src => src.Brand != null))
                .ForMember(dest => dest.Manufacturer,
                    opt => opt.Condition(src => src.Manufacturer != null))
                .ForMember(dest => dest.Description,
                    opt => opt.Condition(src => src.Description != null))
                .ForMember(dest => dest.UnitOfMeasure,
                    opt => opt.Condition(src => src.UnitOfMeasure != null));

            // =========================
            // RESULT
            // =========================
            CreateMap<ProductCatalog, ProductCatalogResult>();
        }
    }
}
