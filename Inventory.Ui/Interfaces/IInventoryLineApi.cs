using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Dto.InventoryLines.Requests;
using Inventory.Dto.InventoryLines.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces;

public interface IInventoryLineApi
{
    [Post("/api/v1/inventoryLines")]
    Task<InventoryLineResult> Create(
        [Body] CreateInventoryLineRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/inventoryLines/{id}")]
    Task<InventoryLineResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/inventoryLines")]
    Task<List<InventoryLineResult>> GetAll(
        CancellationToken cancellationToken = default);

    [Put("/api/v1/inventoryLines/{id}")]
    Task<InventoryLineResult> Update(
        Guid id,
        [Body] UpdateInventoryLineRequest request,
        CancellationToken cancellationToken = default);

    [Delete("/api/v1/inventoryLines/{id}")]
    Task Delete(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/inventoryLines/search")]
    Task<PagedResult<InventoryLineResult>> Search(
        [Query] InventoryLineQuery query,
        CancellationToken cancellationToken = default);
}