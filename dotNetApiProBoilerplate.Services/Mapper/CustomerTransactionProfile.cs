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
                .ForMember(dest => dest.Type,
                    opt => opt.Condition(src => src.Type != null))
                .ForMember(dest => dest.Description,
                    opt => opt.Condition(src => src.Description != null))
                // Value types — toujours mappés
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.BalanceBefore, opt => opt.MapFrom(src => src.BalanceBefore))
                .ForMember(dest => dest.BalanceAfter, opt => opt.MapFrom(src => src.BalanceAfter))
                .ForMember(dest => dest.SaleId, opt => opt.MapFrom(src => src.SaleId));

            // =========================
            // RESULT
            // =========================
            CreateMap<CustomerTransaction, CustomerTransactionResult>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : null));
        }
    }
}
