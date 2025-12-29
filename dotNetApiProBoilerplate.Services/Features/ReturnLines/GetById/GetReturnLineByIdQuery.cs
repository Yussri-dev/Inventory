using Inventory.Dto.ReturnLines.Results;
using Inventory.Services.Features.ReturnLines.GetById;
using MediatR;

namespace Inventory.Services.Features.ReturnLines.GetById
{
    public class GetReturnLineByIdQuery : IRequest<ReturnLineResult>
    {
        public Guid Id { get; }

        public GetReturnLineByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
