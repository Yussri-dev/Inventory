using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.GetById
{
    public class GetSaleByIdQuery : IRequest<SaleResult>
    {
        public Guid Id { get; }

        public GetSaleByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
