using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
using MediatR;


namespace Inventory.Services.Features.CustomerTransactions.RegisterRefund
{
    // Command
    public record RegisterCustomerRefundCommand(RegisterCustomerRefundRequest Request)
        : IRequest<CustomerTransactionResult>;
}
