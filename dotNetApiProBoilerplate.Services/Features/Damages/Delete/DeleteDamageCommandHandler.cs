using MediatR;


namespace Inventory.Services.Features.Damages.Delete
{
    public class DeleteDamageCommandHandler
         : IRequestHandler<DeleteDamageCommand, Unit>
    {
        private readonly DamageService _customerService;

        public DeleteDamageCommandHandler(DamageService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteDamageCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
