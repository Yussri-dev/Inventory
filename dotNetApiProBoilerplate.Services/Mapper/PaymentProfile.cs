using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Payments.Requests;
using Inventory.Dto.Payments.Results;

namespace Inventory.Services.Mapper
{
    public class PaymentProfile : Profile
    {
        public PaymentProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreatePaymentRequest, Payment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PaidAt, opt => opt.Ignore())
                .ForMember(dest => dest.RefundedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsRefunded, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Sale, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdatePaymentRequest, Payment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PaidAt, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Sale, opt => opt.Ignore())

                // Strings — update contrôlé
                .ForMember(dest => dest.TransactionRef,
                    opt => opt.Condition(src => src.TransactionRef != null))
                .ForMember(dest => dest.CardLastFourDigits,
                    opt => opt.Condition(src => src.CardLastFourDigits != null))
                .ForMember(dest => dest.Notes,
                    opt => opt.Condition(src => src.Notes != null))

                // Value types — toujours mappés
                .ForMember(dest => dest.Method, opt => opt.MapFrom(src => src.Method))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.IsRefunded, opt => opt.MapFrom(src => src.IsRefunded))
                .ForMember(dest => dest.RefundedAt, opt => opt.MapFrom(src => src.RefundedAt));

            // =========================
            // RESULT
            // =========================
            CreateMap<Payment, PaymentResult>();
        }
    }
}
