using Inventory.Dto.Payments.Results;
using MediatR;

namespace Inventory.Services.Features.Payments.GetAll
{
    public class GetAllPaymentsQueryHandler
    : IRequestHandler<GetAllPaymentsQuery, List<PaymentResult>>
    {
        private readonly PaymentService _customerService;

        public GetAllPaymentsQueryHandler(PaymentService customerService)
        {
            _customerService = customerService;
        }

        public Task<List<PaymentResult>> Handle(GetAllPaymentsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetAllAsync();
        }
    }
}
