using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CustomerTransactions.Results;
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

                // Navigation
                .ForMember(dest => dest.Customer, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateSaleRequest, Sale>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SaleDate, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Customer, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore())

                // Strings — update contrôlé
                .ForMember(dest => dest.InvoiceNumber,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.InvoiceNumber)));

            CreateMap<Sale, SaleSummaryResult>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src => src.InvoiceNumber))
                .ForMember(dest => dest.SaleDate, opt => opt.MapFrom(src => src.SaleDate))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount))
                .ForMember(dest => dest.PaidAmount, opt => opt.MapFrom(src => src.PaidAmount))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));
            // =========================
            // RESULT
            // =========================
            CreateMap<Sale, SaleResult>()
                    .ForMember(s => s.CustomerName,
                    opt => opt.MapFrom(c => c.Customer.Name));
        }
    }
}
