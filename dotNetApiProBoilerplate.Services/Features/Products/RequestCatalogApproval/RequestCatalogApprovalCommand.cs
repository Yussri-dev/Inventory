using Inventory.Dto.Products.Results;
using MediatR;

namespace Inventory.Services.Features.Products.RequestCatalogApproval
{
    public sealed record RequestCatalogApprovalCommand(Guid ProductId)
        : IRequest<ProductResult>;
}
