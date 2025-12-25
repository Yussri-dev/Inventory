using Inventory.Dto.Payments.Results;
using Inventory.Dto.Payments.Requests;
using Inventory.Services.Features.Payments.Update;
using MediatR;

namespace Inventory.Services.Features.Payments.Update
{
    public class UpdatePaymentCommand : IRequest<PaymentResult>
    {
        public Guid Id { get; }
        public UpdatePaymentRequest Request { get; }

        public UpdatePaymentCommand(Guid id, UpdatePaymentRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
