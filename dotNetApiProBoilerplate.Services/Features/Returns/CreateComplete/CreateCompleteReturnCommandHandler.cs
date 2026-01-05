using Inventory.Dto.Returns.Results;
using MediatR;

namespace Inventory.Services.Features.Returns.CreateComplete
{
    public class CreateCompleteReturnCommandHandler : IRequestHandler<CreateCompleteReturnCommand, ReturnResult>
    {
        private readonly ReturnService _service;

        public CreateCompleteReturnCommandHandler(ReturnService service)
        {
            _service = service;
        }

        public async Task<ReturnResult> Handle(CreateCompleteReturnCommand command, CancellationToken cancellationToken)
        {
            return await _service.CreateCompleteAsync(command.Request);
        }
    }
}
