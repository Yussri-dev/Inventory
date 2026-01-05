using Inventory.Dto.Returns.Requests;
using Inventory.Dto.Returns.Results;
using MediatR;

namespace Inventory.Services.Features.Returns.CreateComplete
{
    public class CreateCompleteReturnCommand : IRequest<ReturnResult>
    {
        public CreateCompleteReturnRequest Request { get; }

        public CreateCompleteReturnCommand(CreateCompleteReturnRequest request)
        {
            Request = request;
        }
    }
}
