using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.LoyaltyCards.Requests;
using Inventory.Dto.LoyaltyCards.Results;

namespace Inventory.Services.Mapping
{
    public class LoyaltyCardProfile : Profile
    {
        public LoyaltyCardProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateLoyaltyCardRequest, LoyaltyCard>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateLoyaltyCardRequest, LoyaltyCard>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // RESULT
            // =========================
            CreateMap<LoyaltyCard, LoyaltyCardResult>();
        }
    }
}
