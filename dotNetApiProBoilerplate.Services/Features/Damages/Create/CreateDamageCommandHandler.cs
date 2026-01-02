using Inventory.Dto.Damages.Results;
using MediatR;

namespace Inventory.Services.Features.Damages.Create
{
    public class CreateDamageCommandHandler : IRequestHandler<CreateDamageCommand, DamageResult>
    {
        private readonly DamageService _customerService;

        public CreateDamageCommandHandler(DamageService productService)
        {
            _customerService = productService;
        }

        public Task<DamageResult> Handle(CreateDamageCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }
}
