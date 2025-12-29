using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.PurchaseLines.Requests;
using Inventory.Dto.PurchaseLines.Results;

namespace Inventory.Services.Mapping
{
    public class PurchaseLineProfile : Profile
    {
        public PurchaseLineProfile()
        {
            //create
            CreateMap<CreatePurchaseLineRequest, PurchaseLine>()
               .ForMember(dest => dest.Id, opt => opt.Ignore())

               // Navigation
               .ForMember(dest => dest.Product, opt => opt.Ignore())
               .ForMember(dest => dest.Purchase, opt => opt.Ignore());

            CreateMap<UpdatePurchaseLineRequest, PurchaseLine>()
               .ForMember(dest => dest.Id, opt => opt.Ignore())

               .ForMember(dest => dest.Product, opt => opt.Ignore())
               .ForMember(dest => dest.Purchase, opt => opt.Ignore());



            // =========================
            // RESULT
            // =========================
            CreateMap<PurchaseLine, PurchaseLineResult>();

        }
    }
}
