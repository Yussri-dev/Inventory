using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Customers.Results;

namespace Inventory.Services.Mapper
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateCustomerRequest, Customer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())

                // Navigation properties
                .ForMember(dest => dest.Sales, opt => opt.Ignore())
                .ForMember(dest => dest.LoyaltyCards, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateCustomerRequest, Customer>()
                // Identity & audit
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())

                // Navigation properties
                .ForMember(dest => dest.Sales, opt => opt.Ignore())
                .ForMember(dest => dest.LoyaltyCards, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore())

                // Strings — update contrôlé
                .ForMember(dest => dest.Name,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Name)))
                .ForMember(dest => dest.Email,
                    opt => opt.Condition(src => src.Email != null))
                .ForMember(dest => dest.Phone,
                    opt => opt.Condition(src => src.Phone != null))
                .ForMember(dest => dest.Address,
                    opt => opt.Condition(src => src.Address != null))
                .ForMember(dest => dest.TaxNumber,
                    opt => opt.Condition(src => src.TaxNumber != null))
                .ForMember(dest => dest.Notes,
                    opt => opt.Condition(src => src.Notes != null))

                // Value types — toujours mappés (déjà validés)
                .ForMember(dest => dest.CreditLimit, opt => opt.MapFrom(src => src.CreditLimit))
                .ForMember(dest => dest.CurrentBalance, opt => opt.MapFrom(src => src.CurrentBalance))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // =========================
            // RESULT
            // =========================
            CreateMap<Customer, CustomerResult>();
        }
    }
}
