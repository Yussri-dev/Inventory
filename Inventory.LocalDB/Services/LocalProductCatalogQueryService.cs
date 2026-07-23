using Inventory.Dto.Enums;
using Inventory.Dto.PackComponent.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Queries;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Inventory.LocalDB.Services;

public sealed class LocalProductCatalogQueryService
    : ILocalProductCatalogQueryService
{
    private readonly PosLocalDbContext _db;

    public LocalProductCatalogQueryService(
        PosLocalDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ProductCatalogResult>> QueryAsync(
        ProductCatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        cancellationToken.ThrowIfCancellationRequested();

        var page = Math.Max(query.Page, 1);

        var pageSize = query.PageSize <= 0
            ? 10
            : Math.Min(query.PageSize, 500);

        var databasePath = _db.Database
            .GetDbConnection()
            .DataSource;

        Debug.WriteLine(
            $"Local ProductCatalog database: {databasePath}");

        var allRowsCount = await _db.ProductCatalogs
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var activeRowsCount = await _db.ProductCatalogs
            .AsNoTracking()
            .CountAsync(
                x => !x.IsDeleted,
                cancellationToken);

        Debug.WriteLine(
            $"Local ProductCatalog rows: " +
            $"total={allRowsCount}, active={activeRowsCount}");

        IQueryable<LocalProductCatalog> dbQuery =
            _db.ProductCatalogs
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var pattern = $"%{search}%";

            dbQuery = dbQuery.Where(x =>
                EF.Functions.Like(x.Name, pattern) ||
                (
                    x.Barcode != null &&
                    EF.Functions.Like(x.Barcode, pattern)
                ) ||
                EF.Functions.Like(x.InternalCode, pattern) ||
                (
                    x.Brand != null &&
                    EF.Functions.Like(x.Brand, pattern)
                ) ||
                (
                    x.Manufacturer != null &&
                    EF.Functions.Like(
                        x.Manufacturer,
                        pattern)
                ));
        }

        dbQuery = ApplySorting(
            dbQuery,
            query.SortBy,
            query.Desc);

        var totalCount = await dbQuery
            .CountAsync(cancellationToken);

        var localItems = await dbQuery
            .Include(x => x.PackComponents)
            .AsSplitQuery()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var results = localItems
            .Select(ToResult)
            .ToList();

        return new PagedResult<ProductCatalogResult>
        {
            Items = results,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductCatalogResult?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        var catalog = await _db.ProductCatalogs
            .AsNoTracking()
            .Include(x => x.PackComponents)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    !x.IsDeleted,
                cancellationToken);

        return catalog == null
            ? null
            : ToResult(catalog);
    }

    public async Task<List<ProductCatalogResult>>
        SearchUnitCatalogsAsync(
            string? search,
            Guid? excludeCatalogId = null,
            int take = 30,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        take = Math.Clamp(take, 1, 100);

        IQueryable<LocalProductCatalog> query =
            _db.ProductCatalogs
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    !x.IsPack &&
                    x.SellingMode == SellingMode.Unit);

        if (excludeCatalogId.HasValue &&
            excludeCatalogId.Value != Guid.Empty)
        {
            query = query.Where(
                x => x.Id != excludeCatalogId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";

            query = query.Where(x =>
                EF.Functions.Like(x.Name, pattern) ||
                (
                    x.Barcode != null &&
                    EF.Functions.Like(
                        x.Barcode,
                        pattern)
                ) ||
                EF.Functions.Like(
                    x.InternalCode,
                    pattern) ||
                (
                    x.Brand != null &&
                    EF.Functions.Like(
                        x.Brand,
                        pattern)
                ));
        }

        var localItems = await query
            .OrderBy(x => x.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        return localItems
            .Select(ToResult)
            .ToList();
    }

    private static IQueryable<LocalProductCatalog> ApplySorting(
        IQueryable<LocalProductCatalog> query,
        string? sortBy,
        bool descending)
    {
        return sortBy?
            .Trim()
            .ToLowerInvariant() switch
        {
            "name" => descending
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name),

            "barcode" => descending
                ? query.OrderByDescending(x => x.Barcode)
                : query.OrderBy(x => x.Barcode),

            "internalcode" => descending
                ? query.OrderByDescending(
                    x => x.InternalCode)
                : query.OrderBy(
                    x => x.InternalCode),

            "brand" => descending
                ? query.OrderByDescending(x => x.Brand)
                : query.OrderBy(x => x.Brand),

            "manufacturer" => descending
                ? query.OrderByDescending(
                    x => x.Manufacturer)
                : query.OrderBy(
                    x => x.Manufacturer),

            "lastsyncedatutc" => descending
                ? query.OrderByDescending(
                    x => x.LastSyncedAtUtc)
                : query.OrderBy(
                    x => x.LastSyncedAtUtc),

            _ => descending
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name)
        };
    }

    private static ProductCatalogResult ToResult(
        LocalProductCatalog catalog)
    {
        return new ProductCatalogResult
        {
            Id = catalog.Id,
            Name = catalog.Name,
            Barcode = catalog.Barcode,
            InternalCode = catalog.InternalCode,
            Brand = catalog.Brand,
            Manufacturer = catalog.Manufacturer,
            Description = catalog.Description,
            CategoryId = catalog.CategoryId,
            SellingMode = catalog.SellingMode,

            UnitOfMeasure =
                string.IsNullOrWhiteSpace(
                    catalog.UnitOfMeasure)
                    ? "pcs"
                    : catalog.UnitOfMeasure,

            IsPack = catalog.IsPack,

            PackComponents = catalog.PackComponents?
                .OrderBy(x => x.ComponentName)
                .Select(x => new PackComponentResult
                {
                    ComponentCatalogId =
                        x.ComponentCatalogId,

                    ComponentName =
                        x.ComponentName,

                    Quantity =
                        x.Quantity
                })
                .ToList()
                ?? new List<PackComponentResult>()
        };
    }

    public Task<bool> HasLocalDataAsync(
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.ProductCatalogs
            .AsNoTracking()
            .AnyAsync(
                x => !x.IsDeleted,
                cancellationToken);
    }
}