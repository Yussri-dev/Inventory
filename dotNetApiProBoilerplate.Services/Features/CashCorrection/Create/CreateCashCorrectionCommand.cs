using Inventory.Dto.CashCorrections.Requests;
using Inventory.Dto.CashCorrections.Results;
using Inventory.Dto.Customers.Results;
using Inventory.Services.Features.Customers.Create;
using MediatR;

namespace Inventory.Services.Features.CashCorrection.Create
{
    
    public class CreateCashCorrectionCommand : IRequest<CashCorrectionResult>
    {
        public CreateCashCorrectionRequest Request { get; }

        public CreateCashCorrectionCommand(CreateCashCorrectionRequest request)
        {
            Request = request;
        }
    }
}
