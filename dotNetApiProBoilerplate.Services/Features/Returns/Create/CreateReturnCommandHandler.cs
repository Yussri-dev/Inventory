using Inventory.Dto.Returns.Results;
using MediatR;

namespace Inventory.Services.Features.Returns.Create
{
    public class CreateReturnCommandHandler : IRequestHandler<CreateReturnCommand, ReturnResult>
    {
        private readonly ReturnService _productService;

        public CreateReturnCommandHandler(ReturnService productService)
        {
            _productService = productService;
        }

        public Task<ReturnResult> Handle(CreateReturnCommand command, CancellationToken cancellationToken)
        {
            // Minimal change: reuse your existing ReturnService logic
            return _productService.CreateAsync(command.Request);
        }
    }
}
