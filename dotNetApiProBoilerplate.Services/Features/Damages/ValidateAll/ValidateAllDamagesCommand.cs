using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Enums;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using MediatR;

namespace Inventory.Services.Features.Damages.ValidateAll
{
    /// <summary>
    /// Validates all draft damages and applies stock deduction.
    /// </summary>
    public class ValidateAllDamagesCommand : IRequest
    {
    }
    public class ValidateAllDamagesCommandHandler
       : IRequestHandler<ValidateAllDamagesCommand>
    {
        private readonly DamageService _service;

        public ValidateAllDamagesCommandHandler(DamageService service)
        {
            _service = service;
        }

        public async Task Handle(
            ValidateAllDamagesCommand request,
            CancellationToken cancellationToken)
        {
            await _service.ValidateAllAsync();
        }
    }
}
