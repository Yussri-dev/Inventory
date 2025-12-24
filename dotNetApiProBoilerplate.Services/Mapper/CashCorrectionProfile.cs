using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CashCorrections.Requests;
using Inventory.Dto.CashCorrections.Results;

namespace Inventory.Services.Mapper
{
    public class CashCorrectionProfile : Profile
    {
        public CashCorrectionProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateCashCorrectionRequest, CashCorrection>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CorrectedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())

                // Navigation properties
                .ForMember(dest => dest.OriginalCashSession, opt => opt.Ignore())
                .ForMember(dest => dest.CorrectedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedByUser, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateCashCorrectionRequest, CashCorrection>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CorrectedAt, opt => opt.Ignore())

                // Navigation properties
                .ForMember(dest => dest.OriginalCashSession, opt => opt.Ignore())
                .ForMember(dest => dest.CorrectedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedByUser, opt => opt.Ignore())

                // Strings — update contrôlé
                .ForMember(dest => dest.Reason,
                    opt => opt.Condition(src => src.Reason != null))
                .ForMember(dest => dest.ApprovalNotes,
                    opt => opt.Condition(src => src.ApprovalNotes != null))

                // Value types — toujours mappés
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.ApprovedAt, opt => opt.MapFrom(src => src.ApprovedAt));

            // =========================
            // RESULT
            // =========================
            CreateMap<CashCorrection, CashCorrectionResult>();
        }
    }
}
