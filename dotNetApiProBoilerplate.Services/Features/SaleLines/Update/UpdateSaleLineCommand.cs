using Inventory.Dto.SaleLines.Results;
using Inventory.Dto.SaleLines.Requests;
using MediatR;

namespace Inventory.Services.Features.SaleLines.Update
{
    public class UpdateSaleLineCommand : IRequest<SaleLineResult>
    {
        public Guid Id { get; }
        public UpdateSaleLineRequest Request { get; }

        public UpdateSaleLineCommand(Guid id, UpdateSaleLineRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
