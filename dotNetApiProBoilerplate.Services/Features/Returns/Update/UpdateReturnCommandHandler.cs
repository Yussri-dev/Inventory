using Inventory.Dto.Returns.Results;
using MediatR;

namespace Inventory.Services.Features.Returns.Update
{
    public class UpdateReturnCommandHandler
       : IRequestHandler<UpdateReturnCommand, ReturnResult>
    {
        private readonly ReturnService _productService;

        public UpdateReturnCommandHandler(ReturnService productService)
        {
            _productService = productService;
        }

        public Task<ReturnResult> Handle(UpdateReturnCommand command, CancellationToken cancellationToken)
        {
            return _productService.UpdateAsync(command.Id, command.Request);
        }
    }
}
