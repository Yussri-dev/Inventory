using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CashReports.Requests;
using Inventory.Dto.CashReports.Results;

namespace Inventory.Services.Mapper
{
    public class CashReportProfile : Profile
    {
        public CashReportProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateCashReportRequest, CashReport>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.GeneratedAt, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.CashSession, opt => opt.Ignore())
                .ForMember(dest => dest.GeneratedByUser, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateCashReportRequest, CashReport>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.GeneratedAt, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.CashSession, opt => opt.Ignore())
                .ForMember(dest => dest.GeneratedByUser, opt => opt.Ignore())

                // Strings — update contrôlé
                .ForMember(dest => dest.Type,
                    opt => opt.Condition(src => src.Type != null))
                .ForMember(dest => dest.Notes,
                    opt => opt.Condition(src => src.Notes != null))

                // Value types — toujours mappés
                .ForMember(dest => dest.ExpectedAmount, opt => opt.MapFrom(src => src.ExpectedAmount))
                .ForMember(dest => dest.CountedAmount, opt => opt.MapFrom(src => src.CountedAmount))
                .ForMember(dest => dest.Difference, opt => opt.MapFrom(src => src.Difference))
                .ForMember(dest => dest.CashSales, opt => opt.MapFrom(src => src.CashSales))
                .ForMember(dest => dest.CardSales, opt => opt.MapFrom(src => src.CardSales))
                .ForMember(dest => dest.OtherPayments, opt => opt.MapFrom(src => src.OtherPayments))
                .ForMember(dest => dest.TotalTransactions, opt => opt.MapFrom(src => src.TotalTransactions));

            // =========================
            // RESULT
            // =========================
            CreateMap<CashReport, CashReportResult>();
        }
    }
}
