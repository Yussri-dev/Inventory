using Inventory.Dto.SaleLines.Results;
using MediatR;


namespace Inventory.Services.Features.SaleLines.GetBySaleId
{
    public record GetSaleLinesBySaleIdQuery(Guid SaleId)
     : IRequest<List<SaleLineResult>>;
}
