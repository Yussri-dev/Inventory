using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.SaleLines.Results;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;

namespace Inventory.Services.Mapping
{
    public class SaleProfile : Profile
    {
        public SaleProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateSaleRequest, Sale>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SaleDate, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateSaleRequest, Sale>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SaleDate, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore())
                .ForMember(dest => dest.InvoiceNumber,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.InvoiceNumber)));

            // =========================
            // SALE LINE RESULT
            // =========================
            CreateMap<SaleLine, SaleLineResult>();

            // =========================
            // SALE RESULT
            // =========================
            CreateMap<Sale, SaleResult>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src =>
                        src.Customer != null ? src.Customer.Name : null))
                .ForMember(dest => dest.SaleLines,
                    opt => opt.MapFrom(src => src.Lines));

            // =========================
            // SALE SUMMARY RESULT
            // =========================
            CreateMap<Sale, SaleSummaryResult>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.PaymentStatus,
                    opt => opt.MapFrom(src => src.PaymentStatus.ToString()));
        }
    }
}