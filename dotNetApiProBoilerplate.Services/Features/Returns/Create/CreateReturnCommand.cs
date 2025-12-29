using Inventory.Dto.Returns.Results;
using Inventory.Dto.Returns.Requests;
using Inventory.Services.Features.Returns.Create;
using MediatR;

namespace Inventory.Services.Features.Returns.Create
{
    public class CreateReturnCommand : IRequest<ReturnResult>
    {
        public CreateReturnRequest Request { get; }

        public CreateReturnCommand(CreateReturnRequest request)
        {
            Request = request;
        }
    }
}
