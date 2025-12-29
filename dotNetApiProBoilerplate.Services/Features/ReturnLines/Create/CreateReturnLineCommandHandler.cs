using Inventory.Dto.ReturnLines.Results;
using MediatR;

namespace Inventory.Services.Features.ReturnLines.Create
{
    public class CreateReturnLineCommandHandler : IRequestHandler<CreateReturnLineCommand, ReturnLineResult>
    {
        private readonly ReturnLineService _productService;

        public CreateReturnLineCommandHandler(ReturnLineService productService)
        {
            _productService = productService;
        }

        public Task<ReturnLineResult> Handle(CreateReturnLineCommand command, CancellationToken cancellationToken)
        {
            return _productService.CreateAsync(command.Request);
        }
    }
}
