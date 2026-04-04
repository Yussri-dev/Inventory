using MediatR;

namespace Inventory.Services.Features.Users.Delete
{
    public class DeleteUserCommandHandler
       : IRequestHandler<DeleteUserCommand, Unit>
    {
        private readonly UserService _productService;

        public DeleteUserCommandHandler(UserService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }

}
