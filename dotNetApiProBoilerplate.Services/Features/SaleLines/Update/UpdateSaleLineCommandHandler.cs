
using Inventory.Dto.SaleLines.Results;
using MediatR;

namespace Inventory.Services.Features.SaleLines.Update
{
    public class UpdateSaleLineCommandHandler
       : IRequestHandler<UpdateSaleLineCommand, SaleLineResult>
    {
        private readonly SaleLineService _productService;

        public UpdateSaleLineCommandHandler(SaleLineService productService)
        {
            _productService = productService;
        }

        public Task<SaleLineResult> Handle(UpdateSaleLineCommand command, CancellationToken cancellationToken)
        {
            return _productService.UpdateAsync(command.Id, command.Request);
        }
    }
}
