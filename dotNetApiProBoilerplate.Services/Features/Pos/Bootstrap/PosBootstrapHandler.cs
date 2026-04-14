using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.GlobalRequests.Results;
using Inventory.Dto.PackComponent.Results;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Stock.Results;
using Inventory.Dto.Suppliers.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services.Features.Pos.Bootstrap
{
    public sealed class PosBootstrapHandler
        : IRequestHandler<PosBootstrapQuery, PosBootstrapResult>
    {
        private readonly IRepository<Product> _products;
        private readonly IRepository<ProductCatalog> _productCatalogs;
        private readonly IRepository<Stock> _stocks;
        private readonly IRepository<Customer> _customers;
        private readonly IRepository<Supplier> _suppliers;
        private readonly ITenantContext _tenant;
        private readonly IMapper _mapper;
        private readonly ICashSessionService _cashSession;

        public PosBootstrapHandler(
            IRepository<Product> products,
            IRepository<ProductCatalog> productCatalog,
            IRepository<Stock> stocks,
            IRepository<Customer> customers,
            IRepository<Supplier> suppliers,
            ITenantContext tenant,
            IMapper mapper,
            ICashSessionService cashSession)
        {
            _products = products;
            _productCatalogs = productCatalog;
            _stocks = stocks;
            _customers = customers;
            _suppliers = suppliers;
            _tenant = tenant;
            _mapper = mapper;
            _cashSession = cashSession;
        }

        public async Task<PosBootstrapResult> Handle(
            PosBootstrapQuery request,
            CancellationToken ct)
        {
            var tenantId = _tenant.TenantId;

            // ── Charger les catalogs AVEC leurs composants pack ──────────────
            var productCatalogs = await _productCatalogs
                .Query()
                .Where(pc => !pc.IsDeleted && pc.TenantId == tenantId)
                .Include(pc => pc.PackComponents)
                    .ThenInclude(comp => comp.ComponentCatalog)
                .ToListAsync(ct);

            // ── Charger les produits ─────────────────────────────────────────
            var products = await _products
                .Query()
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .ToListAsync(ct);

            // ── Charger les stocks ───────────────────────────────────────────
            var stocks = await _stocks.GetAsync(
                s => !s.IsDeleted && s.TenantId == tenantId);

            // ── Charger customers / suppliers ────────────────────────────────
            var customers = await _customers.GetAsync(
                c => !c.IsDeleted && c.TenantId == tenantId);

            var suppliers = await _suppliers.GetAsync(
                s => !s.IsDeleted && s.TenantId == tenantId);

            var activeSession = await _cashSession.GetActiveAsync();

            // ── Construire les dictionnaires pour les lookups ────────────────
            var catalogMap = productCatalogs.ToDictionary(c => c.Id);
            var stockMap = stocks.ToDictionary(s => s.ProductId);

            // Map catalogId → productId (pour trouver le Product de l'unité)
            var catalogToProductMap = products
                .GroupBy(p => p.CatalogProductId)
                .ToDictionary(g => g.Key, g => g.First().Id);

            // ── Construire les ProductResult enrichis ────────────────────────
            var productResults = products.Select(p =>
            {
                catalogMap.TryGetValue(p.CatalogProductId, out var catalog);

                var isPack = catalog?.IsPack ?? false;
                var packSize = 1m;
                Guid? componentProductId = null;

                if (isPack && catalog!.PackComponents.Any())
                {
                    var component = catalog.PackComponents.First();
                    packSize = component.Quantity;

                    if (catalogToProductMap.TryGetValue(component.ComponentCatalogId, out var unitProductId))
                        componentProductId = unitProductId;
                }

                return new ProductResult
                {
                    Id = p.Id,
                    CatalogProductId = p.CatalogProductId,
                    CatalogName = catalog?.Name ?? "",
                    CatalogBarcode = catalog?.Barcode ?? "",
                    SalePrice = p.SalePrice,
                    SalePrice2 = p.SalePrice2,
                    SalePrice3 = p.SalePrice3,
                    PurchasePrice = p.PurchasePrice,
                    VatRate = p.VatRate,
                    MinStockLevel = p.MinStockLevel,
                    MaxStockLevel = p.MaxStockLevel,
                    IsTracked = p.IsTracked,
                    Status = (Dto.Enums.ProductStatus)p.IsActive,
                    IsPack = isPack,
                    PackSize = packSize,
                    ComponentProductId = componentProductId
                };
            }).ToList();

            // ── Construire les ProductCatalogResult ──────────────────────────
            var catalogResults = productCatalogs.Select(c =>
            {
                var result = _mapper.Map<ProductCatalogResult>(c);

                result.IsPack = c.IsPack;
                result.PackComponents = c.PackComponents.Select(pc => new PackComponentResult
                {
                    Id = pc.Id,
                    ComponentCatalogId = pc.ComponentCatalogId,
                    ComponentName = pc.ComponentCatalog?.Name ?? "",
                    ComponentBarCode = pc.ComponentCatalog?.Barcode,
                    Quantity = pc.Quantity
                }).ToList();

                return result;
            }).ToList();

            return new PosBootstrapResult
            {
                ServerTime = DateTime.UtcNow,
                Products = productResults,
                ProductCatalogs = catalogResults,
                Stocks = _mapper.Map<List<StockResult>>(stocks),
                Customers = _mapper.Map<List<CustomerResult>>(customers),
                Suppliers = _mapper.Map<List<SupplierResult>>(suppliers),
                ActiveCashSession = activeSession,
                Config = new PosConfigResult
                {
                    Currency = "EUR",
                    DefaultVatRate = 21,
                    AllowNegativeStock = false
                }
            };
        }
    }
}