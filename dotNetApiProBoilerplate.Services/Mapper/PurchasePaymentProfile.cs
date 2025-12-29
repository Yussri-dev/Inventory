using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.PurchasePayments.Requests;
using Inventory.Dto.PurchasePayments.Results;

namespace Inventory.Services.Mapping
{
    public class PurchasePaymentProfile : Profile
    {
        public PurchasePaymentProfile()
        {
            //Create 
            CreateMap<CreatePurchasePaymentRequest, PurchasePayment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<UpdatePurchasePaymentRequest, PurchasePayment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            //result
            CreateMap<PurchasePayment, PurchasePaymentResult>();
        }
    }
}
