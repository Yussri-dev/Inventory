using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.Promotions.Requests;
using Inventory.Dto.Promotions.Results;

namespace Inventory.Services.Mapping
{
    public class PromotionProfile : Profile
    {
        public PromotionProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreatePromotionRequest, Promotion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdatePromotionRequest, Promotion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // RESULT
            // =========================
            CreateMap<Promotion, PromotionResult>();
        }
    }
}
