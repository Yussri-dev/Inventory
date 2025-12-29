using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.Update
{
    public class UpdateSaleCommandHandler
       : IRequestHandler<UpdateSaleCommand, SaleResult>
    {
        private readonly SaleService _productService;

        public UpdateSaleCommandHandler(SaleService productService)
        {
            _productService = productService;
        }

        public Task<SaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
        {
            return _productService.UpdateAsync(command.Id, command.Request);
        }
    }
}
