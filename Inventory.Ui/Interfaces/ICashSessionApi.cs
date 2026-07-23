using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces;

public interface ICashSessionApi
{
    // =========================
    // GET ACTIVE
    // =========================

    [Get("/api/v1/cashsessions/active")]
    Task<CashSessionResult?> GetActive(
        CancellationToken cancellationToken = default);

    // =========================
    // CREATE / OPEN
    // =========================

    [Post("/api/v1/cashsessions")]
    Task<CashSessionResult> Create(
        [Body] CreateCashSessionRequest request,
        CancellationToken cancellationToken = default);

    // =========================
    // CLOSE
    // =========================

    [Post("/api/v1/cashsessions/{id}/close")]
    Task<CashSessionResult> Close(
        Guid id,
        [Body] CloseCashSessionRequest request,
        CancellationToken cancellationToken = default);

    // =========================
    // GET ALL
    // =========================

    [Get("/api/v1/cashsessions")]
    Task<List<CashSessionResult>> GetAll(
        CancellationToken cancellationToken = default);

    // =========================
    // GET BY ID
    // =========================

    [Get("/api/v1/cashsessions/{id}")]
    Task<CashSessionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default);

    // =========================
    // UPDATE
    // =========================

    [Put("/api/v1/cashsessions/{id}")]
    Task<CashSessionResult> Update(
        Guid id,
        [Body] UpdateCashSessionRequest request,
        CancellationToken cancellationToken = default);

    // =========================
    // DELETE
    // =========================

    [Delete("/api/v1/cashsessions/{id}")]
    Task Delete(
        Guid id,
        CancellationToken cancellationToken = default);

    // =========================
    // SEARCH
    // =========================

    [Get("/api/v1/cashsessions/search")]
    Task<PagedResult<CashSessionResult>> Search(
        [Query] CashSessionQuery query,
        CancellationToken cancellationToken = default);
}   