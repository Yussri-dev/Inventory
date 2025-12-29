using Inventory.Dto.SaleLines.Results;
using MediatR;

namespace Inventory.Services.Features.SaleLines.Create
{
    public class CreateSaleLineCommandHandler : IRequestHandler<CreateSaleLineCommand, SaleLineResult>
    {
        private readonly SaleLineService _productService;

        public CreateSaleLineCommandHandler(SaleLineService productService)
        {
            _productService = productService;
        }

        public Task<SaleLineResult> Handle(CreateSaleLineCommand command, CancellationToken cancellationToken)
        {
            // Minimal change: reuse your existing SaleService logic
            return _productService.CreateAsync(command.Request);
        }
    }
}
