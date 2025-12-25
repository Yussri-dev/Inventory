using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;

namespace Inventory.Services.Mapper
{
    public class CashSessionProfile : Profile
    {
        public CashSessionProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateCashSessionRequest, CashSession>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OpenedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ClosedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Difference, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.OpenedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.ClosedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Sales, opt => opt.Ignore())
                .ForMember(dest => dest.CashMovements, opt => opt.Ignore())
                .ForMember(dest => dest.CashReports, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateCashSessionRequest, CashSession>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OpenedAt, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.OpenedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.ClosedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Sales, opt => opt.Ignore())
                .ForMember(dest => dest.CashMovements, opt => opt.Ignore())
                .ForMember(dest => dest.CashReports, opt => opt.Ignore())

                // Strings — update contrôlé
                .ForMember(dest => dest.OpeningNotes,
                    opt => opt.Condition(src => src.OpeningNotes != null))
                .ForMember(dest => dest.ClosingNotes,
                    opt => opt.Condition(src => src.ClosingNotes != null))

                // Value types — toujours mappés
                .ForMember(dest => dest.SessionNumber, opt => opt.MapFrom(src => src.SessionNumber))
                .ForMember(dest => dest.OpeningAmount, opt => opt.MapFrom(src => src.OpeningAmount))
                .ForMember(dest => dest.ClosingAmountExpected, opt => opt.MapFrom(src => src.ClosingAmountExpected))
                .ForMember(dest => dest.ClosingAmountCounted, opt => opt.MapFrom(src => src.ClosingAmountCounted))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.ClosedAt, opt => opt.MapFrom(src => src.ClosedAt));

            // =========================
            // RESULT
            // =========================
            CreateMap<CashSession, CashSessionResult>();
        }
    }
}
