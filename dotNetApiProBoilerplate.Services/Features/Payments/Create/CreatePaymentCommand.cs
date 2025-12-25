using Inventory.Dto.Payments.Results;
using Inventory.Dto.Payments.Requests;
using Inventory.Services.Features.Payments.Create;
using MediatR;

namespace Inventory.Services.Features.Payments.Create
{
    public class CreatePaymentCommand : IRequest<PaymentResult>
    {
        public CreatePaymentRequest Request { get; }

        public CreatePaymentCommand(CreatePaymentRequest request)
        {
            Request = request;
        }
    }
}
