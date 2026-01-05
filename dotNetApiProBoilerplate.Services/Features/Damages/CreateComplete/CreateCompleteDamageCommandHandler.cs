using Inventory.Dto.Damages.Results;
using MediatR;

namespace Inventory.Services.Features.Damages.CreateComplete
{
    public class CreateCompleteDamageCommandHandler : IRequestHandler<CreateCompleteDamageCommand, DamageResult>
    {
        private readonly DamageService _service;

        public CreateCompleteDamageCommandHandler(DamageService service)
        {
            _service = service;
        }

        public async Task<DamageResult> Handle(CreateCompleteDamageCommand command, CancellationToken cancellationToken)
        {
            return await _service.CreateCompleteAsync(command.Request);
        }
    }
}
