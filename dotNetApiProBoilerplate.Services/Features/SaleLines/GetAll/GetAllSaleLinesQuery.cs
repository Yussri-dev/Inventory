using Inventory.Dto.SaleLines.Results;
using MediatR;

namespace Inventory.Services.Features.SaleLines.GetAll
{
    public class GetAllSaleLinesQuery : IRequest<List<SaleLineResult>>
    {
    }
}
