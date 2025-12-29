using Inventory.Services.Features.Returns.Delete;
using MediatR;

namespace Inventory.Services.Features.Returns.Delete
{
    public class DeleteReturnCommandHandler
         : IRequestHandler<DeleteReturnCommand, Unit>
    {
        private readonly ReturnService _productService;

        public DeleteReturnCommandHandler(ReturnService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeleteReturnCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
