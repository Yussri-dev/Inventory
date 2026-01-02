using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.InventoryLines.Requests;
using Inventory.Dto.InventoryLines.Results;

namespace Inventory.Services.Mapping
{
    public class InventoryLineProfile : Profile
    {
        public InventoryLineProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateInventoryLineRequest, InventoryLine>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateInventoryLineRequest, InventoryLine>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.InventorySessionId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // =========================
            // RESULT
            // =========================
            CreateMap<InventoryLine, InventoryLineResult>();
        }
    }
}
