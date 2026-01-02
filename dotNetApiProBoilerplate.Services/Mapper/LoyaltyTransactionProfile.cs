using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.LoyaltyTransactions.Requests;
using Inventory.Dto.LoyaltyTransactions.Results;

namespace Inventory.Services.Mapping
{
    public class LoyaltyTransactionProfile : Profile
    {
        public LoyaltyTransactionProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateLoyaltyTransactionRequest, LoyaltyTransaction>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateLoyaltyTransactionRequest, LoyaltyTransaction>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.LoyaltyCardId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // RESULT
            // =========================
            CreateMap<LoyaltyTransaction, LoyaltyTransactionResult>();
        }
    }
}
