using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.PackComponent.Results;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Dto.ProductCatalogs.Results;
using System.ComponentModel.DataAnnotations;

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
                .ForMember(dest => dest.TenantProducts, opt => opt.Ignore())
                .ForMember(dest => dest.PackComponents, opt => opt.Ignore())
                .ForMember(dest => dest.UsedInPacks, opt => opt.Ignore())

                // Normalize InternalCode
                .ForMember(dest => dest.InternalCode,
                    opt => opt.MapFrom(src => src.InternalCode.Trim()))

                // Normalize Barcode (optional)
                .ForMember(dest => dest.Barcode,
                    opt => opt.MapFrom(src =>
                        string.IsNullOrWhiteSpace(src.Barcode)
                            ? null
                            : src.Barcode.Trim()));

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateProductCatalogRequest, ProductCatalog>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.TenantProducts, opt => opt.Ignore())
                .ForMember(dest => dest.PackComponents, opt => opt.Ignore())
                .ForMember(dest => dest.UsedInPacks, opt => opt.Ignore())

                // Barcode: allow null (clear) or trimmed value
                .ForMember(dest => dest.Barcode,
                    opt =>
                    {
                        opt.PreCondition(src => src.Barcode != null);
                        opt.MapFrom(src =>
                            string.IsNullOrWhiteSpace(src.Barcode)
                                ? null
                                : src.Barcode.Trim());
                    })

                // InternalCode: must stay valid
                .ForMember(dest => dest.InternalCode,
                    opt =>
                    {
                        opt.PreCondition(src => src.InternalCode != null);
                        opt.MapFrom(src => src.InternalCode.Trim());
                    })

                // Safe updates
                .ForMember(dest => dest.Name,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Name)))
                .ForMember(dest => dest.Brand,
                    opt => opt.Condition(src => src.Brand != null))
                .ForMember(dest => dest.Manufacturer,
                    opt => opt.Condition(src => src.Manufacturer != null))
                .ForMember(dest => dest.Description,
                    opt => opt.Condition(src => src.Description != null))
                .ForMember(dest => dest.UnitOfMeasure,
                    opt => opt.Condition(src => src.UnitOfMeasure != null))
                .ForMember(dest => dest.SellingMode,
                    opt => opt.Condition(src => src.SellingMode != null));

            // =========================
            // RESULT
            // =========================
            CreateMap<ProductCatalog, ProductCatalogResult>()
                .ForMember(dest => dest.InternalCode,
                    opt => opt.MapFrom(src => src.InternalCode))
                .ForMember(dest => dest.Barcode,
                    opt => opt.MapFrom(src => src.Barcode))
                .ForMember(dest => dest.SellingMode,
                    opt => opt.MapFrom(src => src.SellingMode))
                .ForMember(dest => dest.UnitOfMeasure,
                    opt => opt.MapFrom(src => src.UnitOfMeasure))
                .ForMember(dest => dest.IsPack,
                    opt => opt.MapFrom(src => src.IsPack))
                .ForMember(dest => dest.PackComponents,
                    opt => opt.MapFrom(src => src.PackComponents));

            // =========================
            // PACK COMPONENT RESULT
            // =========================
            CreateMap<PackComponent, PackComponentResult>()
                .ForMember(dest => dest.ComponentName,
                    opt => opt.MapFrom(src => src.ComponentCatalog != null
                        ? src.ComponentCatalog.Name
                        : string.Empty))
                .ForMember(dest => dest.ComponentBarCode,
                    opt => opt.MapFrom(src => src.ComponentCatalog != null
                        ? src.ComponentCatalog.Barcode
                        : null))
                .ForMember(dest => dest.ComponentInternalCode,
                    opt => opt.MapFrom(src => src.ComponentCatalog != null
                        ? src.ComponentCatalog.InternalCode
                        : null));
        }
    }
}