using Inventory.Dto.PurchasePayments.Results;
using Inventory.Services.Features.PurchasePayments.GetAll;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.GetAll
{
    public class GetAllPurchasePaymentsQuery : IRequest<List<PurchasePaymentResult>>
    {
    }

}
