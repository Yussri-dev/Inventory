using Inventory.Dto.ReturnLines.Results;
using Inventory.Dto.ReturnLines.Requests;
using Inventory.Services.Features.ReturnLines.Update;
using MediatR;

namespace Inventory.Services.Features.ReturnLines.Update
{
    public class UpdateReturnLineCommand : IRequest<ReturnLineResult>
    {
        public Guid Id { get; }
        public UpdateReturnLineRequest Request { get; }

        public UpdateReturnLineCommand(Guid id, UpdateReturnLineRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
