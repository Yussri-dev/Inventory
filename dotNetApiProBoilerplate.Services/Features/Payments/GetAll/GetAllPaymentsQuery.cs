using Inventory.Dto.Payments.Results;
using Inventory.Services.Features.Payments.GetAll;
using MediatR;

namespace Inventory.Services.Features.Payments.GetAll
{
    public class GetAllPaymentsQuery : IRequest<List<PaymentResult>>
    {
    }
}
