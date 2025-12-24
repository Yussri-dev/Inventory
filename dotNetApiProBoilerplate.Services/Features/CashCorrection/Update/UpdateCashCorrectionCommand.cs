using Inventory.Dto.CashCorrections.Requests;
using Inventory.Dto.CashCorrections.Results;
using Inventory.Dto.Customers.Results;
using Inventory.Services.Features.Customers.Update;
using MediatR;

namespace Inventory.Services.Features.CashCorrection.Update
{
    public class UpdateCashCorrectionCommand : IRequest<CashCorrectionResult>
    {
        public Guid Id { get; }
        public UpdateCashCorrectionRequest Request { get; }

        public UpdateCashCorrectionCommand(Guid id, UpdateCashCorrectionRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
