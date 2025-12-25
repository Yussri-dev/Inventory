using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Services.Features.ProductCatalogs.Create;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.ProductCatalogs.Create
{
    public class CreateProductCatalogCommand : IRequest<ProductCatalogResult>
    {
        public CreateProductCatalogRequest Request { get; }

        public CreateProductCatalogCommand(CreateProductCatalogRequest request)
        {
            Request = request;
        }
    }
}
