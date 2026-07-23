using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces;

public interface IProductApi
{
    [Post("/api/v1/products")]
    Task<ProductResult> Create(
        [Body] CreateProductRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/products")]
    Task<List<ProductResult>> GetAll(
        CancellationToken cancellationToken = default);

    [Get("/api/v1/products/{id}")]
    Task<ProductResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default);

    [Put("/api/v1/products/{id}")]
    Task<ProductResult> Update(
        Guid id,
        [Body] UpdateProductRequest request,
        CancellationToken cancellationToken = default);

    [Delete("/api/v1/products/{id}")]
    Task Delete(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/products/search")]
    Task<PagedResult<ProductResult>> Search(
        [Query] ProductQuery query,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/products/{id}/label")]
    Task<HttpResponseMessage> GetLabel(
        Guid id,
        CancellationToken cancellationToken = default);
}