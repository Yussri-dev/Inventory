using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.ProductCategory.Requests;
using Inventory.Dto.ProductCategory.Results;

namespace Inventory.Services.Mapper
{
    public class ProductCategoryProfile : Profile
    {
        public ProductCategoryProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateProductCategoryRequest, ProductCategory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore())
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .ForMember(dest => dest.Color, opt => opt.Ignore())
                .ForMember(dest => dest.Icon, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateProductCategoryRequest, ProductCategory>()
                // Identity & audit
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())

                // Navigation properties
                .ForMember(dest => dest.Products, opt => opt.Ignore())

                // Strings — update contrôlé
                .ForMember(dest => dest.Name,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Name)))
                .ForMember(dest => dest.Color,
                    opt => opt.Condition(src => src.Color != null))
                .ForMember(dest => dest.Icon,
                    opt => opt.Condition(src => src.Icon != null));

            // =========================
            // RESULT
            // =========================
            CreateMap<ProductCategory, ProductCategoryResult>();
        }
    }
}
