using Inventory.Dto.Damages.Results;
using MediatR;

namespace Inventory.Services.Features.Damages.Update
{
    public class UpdateDamageCommandHandler
       : IRequestHandler<UpdateDamageCommand, DamageResult>
    {
        private readonly DamageService _customerService;

        public UpdateDamageCommandHandler(DamageService customerService)
        {
            _customerService = customerService;
        }

        public Task<DamageResult> Handle(UpdateDamageCommand command, CancellationToken cancellationToken)
        {
            return _customerService.UpdateAsync(command.Id, command.Request);
        }
    }


}
