using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Purchases.Results;

namespace Inventory.Services.Mapping
{
    public class PurchaseProfile : Profile
    {
        public PurchaseProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreatePurchaseRequest, Purchase>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseDate, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmountExclVat, opt => opt.Ignore())
                .ForMember(dest => dest.TotalVatAmount, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmountInclVat, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore())
                .ForMember(dest => dest.Payments, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdatePurchaseRequest, Purchase>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseDate, opt => opt.Ignore())

                // Navigation
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.Lines, opt => opt.Ignore())
                .ForMember(dest => dest.Payments, opt => opt.Ignore())

                // Strings — update contrôlé
                .ForMember(dest => dest.PurchaseNumber,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.PurchaseNumber)))
                .ForMember(dest => dest.SupplierInvoiceNumber,
                    opt => opt.Condition(src => src.SupplierInvoiceNumber != null))
                .ForMember(dest => dest.Notes,
                    opt => opt.Condition(src => src.Notes != null))

                // Value types — toujours mappés
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.ExpectedDeliveryDate, opt => opt.MapFrom(src => src.ExpectedDeliveryDate))
                .ForMember(dest => dest.DeliveryDate, opt => opt.MapFrom(src => src.DeliveryDate))
                .ForMember(dest => dest.PaymentDueDate, opt => opt.MapFrom(src => src.PaymentDueDate))
                .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.PaymentDate));

            // =========================
            // RESULT
            // =========================
            CreateMap<Purchase, PurchaseResult>();
        }
    }
}
