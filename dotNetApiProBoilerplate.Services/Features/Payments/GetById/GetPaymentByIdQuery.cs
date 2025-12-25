using Inventory.Dto.Payments.Results;
using MediatR;

namespace Inventory.Services.Features.Payments.GetById
{
    public class GetPaymentByIdQuery : IRequest<PaymentResult>
    {
        public Guid Id { get; }

        public GetPaymentByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
