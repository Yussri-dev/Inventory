using Inventory.Dto.CashCorrections.Results;
using MediatR;

namespace Inventory.Services.Features.CashCorrection.GetAll
{
    public class GetAllCashCorrectionsQuery : IRequest<List<CashCorrectionResult>>
    {
    }
}
