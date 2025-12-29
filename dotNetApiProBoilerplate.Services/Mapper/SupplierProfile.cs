using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Dto.Suppliers.Results;

namespace Inventory.Services.Mapping
{
    public class SupplierProfile : Profile
    {
        public SupplierProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateSupplierRequest, Supplier>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())

                // Navigation properties
                .ForMember(dest => dest.Purchases, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierReturns, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateSupplierRequest, Supplier>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())

                // Navigation properties
                .ForMember(dest => dest.Purchases, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierReturns, opt => opt.Ignore())

                // Strings: update only if provided
                .ForMember(dest => dest.Name,
                    opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Name)))
                .ForMember(dest => dest.ContactPerson,
                    opt => opt.Condition(src => src.ContactPerson != null))
                .ForMember(dest => dest.Email,
                    opt => opt.Condition(src => src.Email != null))
                .ForMember(dest => dest.Phone,
                    opt => opt.Condition(src => src.Phone != null))
                .ForMember(dest => dest.Address,
                    opt => opt.Condition(src => src.Address != null))
                .ForMember(dest => dest.City,
                    opt => opt.Condition(src => src.City != null))
                .ForMember(dest => dest.Country,
                    opt => opt.Condition(src => src.Country != null))
                .ForMember(dest => dest.PostalCode,
                    opt => opt.Condition(src => src.PostalCode != null))
                .ForMember(dest => dest.TaxNumber,
                    opt => opt.Condition(src => src.TaxNumber != null))
                .ForMember(dest => dest.BankAccount,
                    opt => opt.Condition(src => src.BankAccount != null))
                .ForMember(dest => dest.Notes,
                    opt => opt.Condition(src => src.Notes != null));

            // =========================
            // RESULT
            // =========================
            CreateMap<Supplier, SupplierResult>();
        }
    }
}
