
using Inventory.Dto.SaleLines.Results;
using Inventory.Services.Features.SaleLines.GetById;
using MediatR;

namespace Inventory.Services.Features.SaleLines.GetById
{
    public class GetSaleLineByIdQuery : IRequest<SaleLineResult>
    {
        public Guid Id { get; }

        public GetSaleLineByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
