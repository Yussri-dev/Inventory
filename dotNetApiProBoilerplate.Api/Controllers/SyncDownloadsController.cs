using System.Data;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Suppliers.Results;
using Inventory.Dto.Stock.Results;
using Inventory.Dto.Damages.Results;
using Inventory.Dto.Sync;
using Inventory.Infrastructure.Data;
using Inventory.Services.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Controllers;

[ApiController, ApiVersionNeutral, Authorize, Route("api/sync-downloads")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SyncDownloadsController(InventoryDbContext db, ITenantContext tenant, IMapper mapper) : ControllerBase
{
    private IQueryable<T> Store<T>() where T : class
    {
        if (tenant.TenantId == Guid.Empty) throw new UnauthorizedAccessException("A store must be selected.");
        return db.Set<T>().AsNoTracking().Where(x => EF.Property<Guid>(x, "TenantId") == tenant.TenantId);
    }

    [HttpGet("products/debug-result/{barcode}")]
    public async Task<IActionResult> DebugProductResult(
    string barcode,
    CancellationToken ct)
    {
        var product = await Store<Product>()
            .Where(x =>
                !x.IsDeleted &&
                x.Barcode == barcode)
            .ProjectTo<ProductResult>(
                mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpGet("products/debug/{barcode}")]
    public async Task<IActionResult> DebugProduct(
    string barcode,
    CancellationToken ct)
    {
        var product = await Store<Product>()
            .Where(x =>
                !x.IsDeleted &&
                x.Barcode == barcode)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Barcode,
                x.PurchasePrice,
                x.SalePrice,
                x.SalePrice2,
                x.SalePrice3,
                x.VatRate,
                x.TenantId
            })
            .FirstOrDefaultAsync(ct);

        if (product == null)
            return NotFound();

        return Ok(product);
    }
    // RepeatableRead keeps rows and tombstones coherent, including nested projections.
    // Each full refresh was already buffered by the client. One response avoids offset-page drift.
    private async Task<SyncDownload<TResult>> Read<T, TResult>(IQueryable<T> query, CancellationToken ct) where T : class
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var result = new SyncDownload<TResult>
        {
            DeletedIds = await query.Where(x => EF.Property<bool>(x, "IsDeleted"))
                .Select(x => EF.Property<Guid>(x, "Id")).ToListAsync(ct),
            Items = await query.Where(x => !EF.Property<bool>(x, "IsDeleted"))
                .OrderBy(x => EF.Property<Guid>(x, "Id")).ProjectTo<TResult>(mapper.ConfigurationProvider).ToListAsync(ct)
        };
        await tx.CommitAsync(ct);
        return result;
    }

    [HttpGet("customers")]
    public Task<SyncDownload<CustomerResult>> Customers(CancellationToken ct) => Read<Customer, CustomerResult>(Store<Customer>(), ct);
    [HttpGet("products")]
    public Task<SyncDownload<ProductResult>> Products(CancellationToken ct) => Read<Product, ProductResult>(Store<Product>(), ct);
    [HttpGet("suppliers")]
    public Task<SyncDownload<SupplierResult>> Suppliers(CancellationToken ct) => Read<Supplier, SupplierResult>(Store<Supplier>(), ct);
    [HttpGet("stocks")]
    public Task<SyncDownload<StockResult>> Stocks(CancellationToken ct) => Read<Stock, StockResult>(Store<Stock>().Where(x => !x.Product.IsDeleted), ct);
    [HttpGet("damages")]
    public Task<SyncDownload<DamageResult>> Damages(CancellationToken ct) => Read<Damage, DamageResult>(Store<Damage>(), ct);
    [HttpGet("catalogs")]
    public Task<SyncDownload<ProductCatalogResult>> Catalogs(CancellationToken ct) => Read<ProductCatalog, ProductCatalogResult>(db.Set<ProductCatalog>().AsNoTracking(), ct);
}
