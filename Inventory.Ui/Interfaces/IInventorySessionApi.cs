using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Dto.InventorySessions.Requests;
using Inventory.Dto.InventorySessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces;

public interface IInventorySessionApi
{
    [Post("/api/v1/inventorySessions")]
    Task<InventorySessionResult> Create(
        [Body] CreateInventorySessionRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/inventorySessions/{id}")]
    Task<InventorySessionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/inventorySessions")]
    Task<List<InventorySessionResult>> GetAll(
        CancellationToken cancellationToken = default);

    [Put("/api/v1/inventorySessions/{id}")]
    Task<InventorySessionResult> Update(
        Guid id,
        [Body] UpdateInventorySessionRequest request,
        CancellationToken cancellationToken = default);

    [Delete("/api/v1/inventorySessions/{id}")]
    Task Delete(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/inventorySessions/search")]
    Task<PagedResult<InventorySessionResult>> Search(
        [Query] InventorySessionQuery query,
        CancellationToken cancellationToken = default);

    [Post("/api/v1/inventorySessions/{id}/close")]
    Task Close(
        Guid id,
        CancellationToken cancellationToken = default);

    [Post("/api/v1/inventorySessions/{id}/validate")]
    Task Validate(
        Guid id,
        CancellationToken cancellationToken = default);
}