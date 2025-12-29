using MediatR;


namespace Inventory.Services.Features.ReturnLines.Delete
{
    public class DeleteReturnLineCommandHandler
       : IRequestHandler<DeleteReturnLineCommand, Unit>
    {
        private readonly ReturnLineService _productService;

        public DeleteReturnLineCommandHandler(ReturnLineService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeleteReturnLineCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
