using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.Damages.Requests;
using Inventory.Dto.Damages.Results;

namespace Inventory.Services.Mapping
{
    public class DamageProfile : Profile
    {
        public DamageProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateDamageRequest, Damage>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.DamageDate, opt => opt.Ignore())
                //.ForMember(dest => dest.IsApproved, opt => opt.Ignore())
                //.ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
                //.ForMember(dest => dest.ApprovedByUserId, opt => opt.Ignore()

                // Navigation
                .ForMember(dest => dest.Product, opt => opt.Ignore());

            CreateMap<CreateCompleteDamageRequest, Damage>()
                .IncludeBase<CreateDamageRequest, Damage>();

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateDamageRequest, Damage>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.DamageDate, opt => opt.Ignore())
                //.ForMember(dest => dest.IsApproved, opt => opt.Ignore())
                //.ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
                //.ForMember(dest => dest.ApprovedByUserId, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Product, opt => opt.Ignore())

                // Controlled updates
                .ForMember(dest => dest.Reason,
                    opt => opt.Condition(src => src.Reason != null))
                .ForMember(dest => dest.Category,
                    opt => opt.Condition(src => src.Category != null))
                //.ForMember(dest => dest.Photos,
                //    opt => opt.Condition(src => src.Photos != null))
                //.ForMember(dest => dest.Notes,
                //    opt => opt.Condition(src => src.Notes != null))
                ;

            // =========================
            // RESULT
            // =========================
            CreateMap<Damage, DamageResult>()
                .ForMember(d => d.ProductName,
                    opt => opt.MapFrom(s => s.Product.Name));

        }
    }
}
