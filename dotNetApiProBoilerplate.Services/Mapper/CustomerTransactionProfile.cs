using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;

namespace Inventory.Services.Mapper
{
    public class CustomerTransactionProfile : Profile
    {
        public CustomerTransactionProfile()
        {
            // =========================
            // CREATE
            // =========================
            CreateMap<CreateCustomerTransactionRequest, CustomerTransaction>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TransactionDate, opt => opt.Ignore())
                // Navigation
                .ForMember(dest => dest.Customer, opt => opt.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<UpdateCustomerTransactionRequest, CustomerTransaction>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TransactionDate, opt => opt.Ignore())
                // Navigation
                .ForMember(dest => dest.Customer, opt => opt.Ignore())
                // Strings — update contrôlé
                .ForMember(dest => dest.Description,
                    opt => opt.Condition(src => src.Description != null));

            // =========================
            // RESULT
            // =========================
            CreateMap<CustomerTransaction, CustomerTransactionResult>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : null));
        }
    }
}
