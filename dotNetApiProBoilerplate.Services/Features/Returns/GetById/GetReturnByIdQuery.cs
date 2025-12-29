using Inventory.Dto.Returns.Results;
using Inventory.Services.Features.Returns.GetById;
using MediatR;

namespace Inventory.Services.Features.Returns.GetById
{
    public class GetReturnByIdQuery : IRequest<ReturnResult>
    {
        public Guid Id { get; }

        public GetReturnByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
