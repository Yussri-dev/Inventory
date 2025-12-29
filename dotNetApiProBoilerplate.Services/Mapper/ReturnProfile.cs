using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Returns.Requests;
using Inventory.Dto.Returns.Results;

namespace Inventory.Services.Mapping
{
    public class ReturnProfile : Profile
    {
        public ReturnProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateReturnRequest, Return>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ReturnDate, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Sale, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateReturnRequest, Return>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ReturnDate, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Sale, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore())

                // Strings — update contrôlé
                .ForMember(dest => dest.ReturnNumber,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.ReturnNumber)))

                // Value types — toujours mappés
                .ForMember(dest => dest.IsProcessed, opt => opt.MapFrom(src => src.IsProcessed));

            // =========================
            // RESULT
            // =========================
            CreateMap<Return, ReturnResult>();
        }
    }
}
