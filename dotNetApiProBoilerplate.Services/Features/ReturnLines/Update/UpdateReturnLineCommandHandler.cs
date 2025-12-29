using Inventory.Dto.ReturnLines.Results;
using MediatR;

namespace Inventory.Services.Features.ReturnLines.Update
{
    public class UpdateReturnLineCommandHandler
       : IRequestHandler<UpdateReturnLineCommand, ReturnLineResult>
    {
        private readonly ReturnLineService _productService;

        public UpdateReturnLineCommandHandler(ReturnLineService productService)
        {
            _productService = productService;
        }

        public Task<ReturnLineResult> Handle(UpdateReturnLineCommand command, CancellationToken cancellationToken)
        {
            return _productService.UpdateAsync(command.Id, command.Request);
        }
    }
}
