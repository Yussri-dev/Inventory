using Inventory.Dto.SaleLines.Results;
using MediatR;


namespace Inventory.Services.Features.SaleLines.GetBySaleId
{
    public record GetSaleLinesWithReturnsQuery(Guid SaleId)
     : IRequest<List<SaleLineWithReturnsResult>>;
}
