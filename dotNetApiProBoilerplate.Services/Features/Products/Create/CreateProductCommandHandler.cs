using Inventory.Dto.Products.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Products.Create
{
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResult>
    {
        private readonly ProductService _productService;

        public CreateProductCommandHandler(ProductService productService)
        {
            _productService = productService;
        }

        public Task<ProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken)
        {
            // Minimal change: reuse your existing ProductService logic
            return _productService.CreateAsync(command.Request);
        }
    }
}
