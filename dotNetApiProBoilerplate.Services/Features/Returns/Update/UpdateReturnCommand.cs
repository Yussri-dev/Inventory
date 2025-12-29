using Inventory.Dto.Returns.Results;
using Inventory.Dto.Returns.Requests;
using MediatR;

namespace Inventory.Services.Features.Returns.Update
{
    public class UpdateReturnCommand : IRequest<ReturnResult>
    {
        public Guid Id { get; }
        public UpdateReturnRequest Request { get; }

        public UpdateReturnCommand(Guid id, UpdateReturnRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
