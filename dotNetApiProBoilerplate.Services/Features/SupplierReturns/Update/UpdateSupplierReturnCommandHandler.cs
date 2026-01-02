using Inventory.Dto.SupplierReturns.Results;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.Update
{
    public class UpdateSupplierReturnCommandHandler
    : IRequestHandler<UpdateSupplierReturnCommand, SupplierReturnResult>
    {
        private readonly SupplierReturnService _customerService;

        public UpdateSupplierReturnCommandHandler(SupplierReturnService customerService)
        {
            _customerService = customerService;
        }

        public Task<SupplierReturnResult> Handle(UpdateSupplierReturnCommand command, CancellationToken cancellationToken)
        {
            return _customerService.UpdateAsync(command.Id, command.Request);
        }
    }
}
