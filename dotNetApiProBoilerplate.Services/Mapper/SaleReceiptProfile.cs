using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.SaleReceipts.Requests;
using Inventory.Dto.SaleReceipts.Results;

namespace Inventory.Services.Mapping
{
    public class SaleReceiptProfile : Profile
    {
        public SaleReceiptProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateSaleReceiptRequest, SaleReceipt>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateSaleReceiptRequest, SaleReceipt>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.SaleId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // RESULT
            // =========================
            CreateMap<SaleReceipt, SaleReceiptResult>();
        }
    }
}
