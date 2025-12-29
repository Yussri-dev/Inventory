using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.Create
{
    public class CreateSaleCommandHandler : IRequestHandler<CreateSaleCommand, SaleResult>
    {
        private readonly SaleService _productService;

        public CreateSaleCommandHandler(SaleService productService)
        {
            _productService = productService;
        }

        public Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
        {
            // Minimal change: reuse your existing SaleService logic
            return _productService.CreateAsync(command.Request);
        }
    }
}
