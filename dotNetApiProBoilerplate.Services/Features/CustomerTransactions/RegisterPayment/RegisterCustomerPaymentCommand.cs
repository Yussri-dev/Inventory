using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
using MediatR;


namespace Inventory.Services.Features.CustomerTransactions.RegisterPayment
{
    public record RegisterCustomerPaymentCommand(
       RegisterCustomerPaymentRequest Request
   ) : IRequest<CustomerTransactionResult>;
}
