using Inventory.Dto.Products.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Products.GetAll
{
    public class GetAllProductsQueryHandler
        : IRequestHandler<GetAllProductsQuery, List<ProductResult>>
    {
        private readonly ProductService _productService;

        public GetAllProductsQueryHandler(ProductService productService)
        {
            _productService = productService;
        }

        public Task<List<ProductResult>> Handle(GetAllProductsQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetAllAsync();
        }
    }
}
