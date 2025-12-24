using Inventory.Dto.CashCorrections.Results;
using Inventory.Dto.Customers.Results;
using Inventory.Services.Features.Customers.GetById;
using MediatR;

namespace Inventory.Services.Features.CashCorrection.GetById
{
    
    public class GetCashCorrectionByIdQuery : IRequest<CashCorrectionResult>
    {
        public Guid Id { get; }

        public GetCashCorrectionByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
