using Inventory.Dto.ReturnLines.Results;
using Inventory.Dto.ReturnLines.Requests;
using Inventory.Services.Features.ReturnLines.Create;
using MediatR;

namespace Inventory.Services.Features.ReturnLines.Create
{
    public class CreateReturnLineCommand : IRequest<ReturnLineResult>
    {
        public CreateReturnLineRequest Request { get; }

        public CreateReturnLineCommand(CreateReturnLineRequest request)
        {
            Request = request;
        }
    }
}
