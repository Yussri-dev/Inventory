using AutoMapper;
using Inventory.Domain.Entities;
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

            // =========================
            // RESULT
            // =========================
            CreateMap<Sale, SaleResult>();
        }
    }
}
