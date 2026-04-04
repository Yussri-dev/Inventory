using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.SaleLines.Requests;
using Inventory.Dto.SaleLines.Results;
using Inventory.Dto.Sales.Results;

namespace Inventory.Services.Mapping
{
    public class SaleLineProfile : Profile
    {
        public SaleLineProfile()
        {
            //create
            CreateMap<CreateSaleLineRequest, SaleLine>()
               .ForMember(dest => dest.Id, opt => opt.Ignore())

               // Navigation
               .ForMember(dest => dest.Product, opt => opt.Ignore())
               .ForMember(dest => dest.Sale, opt => opt.Ignore());

            CreateMap<UpdateSaleLineRequest, SaleLine>()
               .ForMember(dest => dest.Id, opt => opt.Ignore())

               .ForMember(dest => dest.Product, opt => opt.Ignore())
               .ForMember(dest => dest.Sale, opt => opt.Ignore());



            // =========================
            // RESULT
            // =========================
            CreateMap<SaleLine, SaleLineResult>();

            CreateMap<Sale, SaleResult>()
                .ForMember(dest => dest.SaleLines,
                opt => opt.MapFrom(src => src.Lines));
        }
    }
}
