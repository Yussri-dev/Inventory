using Inventory.Dto.Payments.Results;
using MediatR;

namespace Inventory.Services.Features.Payments.GetById
{
    public class GetPaymentByIdQueryHandler
        : IRequestHandler<GetPaymentByIdQuery, PaymentResult>
    {
        private readonly PaymentService _customerService;

        public GetPaymentByIdQueryHandler(PaymentService customerService)
        {
            _customerService = customerService;
        }

        public Task<PaymentResult> Handle(GetPaymentByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
