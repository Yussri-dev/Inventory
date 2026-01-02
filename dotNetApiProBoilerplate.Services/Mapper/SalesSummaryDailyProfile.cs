using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.SalesSummaryDaily.Requests;
using Inventory.Dto.SalesSummaryDaily.Results;

namespace Inventory.Services.Mapping
{
    public class SalesSummaryDailyProfile : Profile
    {
        public SalesSummaryDailyProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateSalesSummaryDailyRequest, SalesSummaryDaily>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateSalesSummaryDailyRequest, SalesSummaryDaily>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // RESULT
            // =========================
            CreateMap<SalesSummaryDaily, SalesSummaryDailyResult>();
        }
    }
}
