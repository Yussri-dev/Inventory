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
            CreateMap<
                    CreateCustomerTransactionRequest,
                    CustomerTransaction>()
                .ForMember(
                    destination => destination.Id,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.ClientOperationId,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.TransactionDate,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.CashSessionId,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.IsCash,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.Customer,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.CashSession,
                    options => options.Ignore());

            // =========================
            // UPDATE
            // =========================
            CreateMap<
                    UpdateCustomerTransactionRequest,
                    CustomerTransaction>()
                .ForMember(
                    destination => destination.Id,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.ClientOperationId,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.TransactionDate,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.CashSessionId,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.IsCash,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.Customer,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.CashSession,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.Description,
                    options => options.Condition(
                        source => source.Description != null));

            // =========================
            // RESULT
            // =========================
            CreateMap<
                    CustomerTransaction,
                    CustomerTransactionResult>()
                .ForMember(
                    destination => destination.CustomerName,
                    options => options.MapFrom(
                        source =>
                            source.Customer != null
                                ? source.Customer.Name
                                : string.Empty));
        }
    }
}