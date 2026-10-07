using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.GlobalRequests.Results;
using Inventory.Dto.PackComponent.Results;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.ProductCategory.Results;
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
        private readonly IRepository<Domain.Entities.ProductCategory> _productCategory;
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
            IRepository<Domain.Entities.ProductCategory> productCategory,
            ITenantContext tenant,
            IMapper mapper,
            ICashSessionService cashSession)
        {
            _products = products;
            _productCatalogs = productCatalog;
            _productCategory = productCategory;
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
            var tenantId =
                _tenant.TenantId;

            // ============================================================
            // PRODUCTS
            // ============================================================

            var products =
                await _products
                    .Query()
                    .Where(p =>
                        !p.IsDeleted &&
                        p.TenantId == tenantId)
                    .ToListAsync(ct);

            // Seulement les vrais CatalogProductId.
            // Les produits custom ont CatalogProductId = null.
            var catalogIds =
                products
                    .Where(p =>
                        p.CatalogProductId.HasValue &&
                        p.CatalogProductId.Value != Guid.Empty)
                    .Select(p =>
                        p.CatalogProductId!.Value)
                    .Distinct()
                    .ToList();

            // ============================================================
            // PRODUCT CATALOGS
            // ============================================================

            var productCatalogs =
                await _productCatalogs
                    .Query()
                    .Where(pc =>
                        !pc.IsDeleted &&
                        catalogIds.Contains(pc.Id))
                    .Include(pc => pc.PackComponents)
                        .ThenInclude(comp =>
                            comp.ComponentCatalog)
                    .ToListAsync(ct);

            // ============================================================
            // PRODUCT CATEGORIES
            // ============================================================

            var productCategories =
                await _productCategory
                    .Query()
                    .Where(p =>
                        !p.IsDeleted)
                    .ToListAsync(ct);

            // ============================================================
            // STOCKS
            // ============================================================

            var stocks =
                await _stocks.GetAsync(
                    s =>
                        !s.IsDeleted &&
                        s.TenantId == tenantId);

            // ============================================================
            // CUSTOMERS
            // ============================================================

            var customers =
                await _customers.GetAsync(
                    c =>
                        !c.IsDeleted &&
                        c.TenantId == tenantId);

            // ============================================================
            // SUPPLIERS
            // ============================================================

            var suppliers =
                await _suppliers.GetAsync(
                    s =>
                        !s.IsDeleted &&
                        s.TenantId == tenantId);

            // ============================================================
            // CASH SESSION
            // ============================================================

            var activeSession =
                await _cashSession.GetActiveAsync();

            // ============================================================
            // LOOKUP MAPS
            // ============================================================

            var catalogMap =
                productCatalogs
                    .ToDictionary(
                        c => c.Id);

            var stockMap =
                stocks
                    .ToDictionary(
                        s => s.ProductId);

            /*
             * CatalogProductId -> ProductId
             *
             * Important :
             * les produits custom ne participent jamais à cette map,
             * car CatalogProductId == null.
             */
            var catalogToProductMap =
                products
                    .Where(p =>
                        p.CatalogProductId.HasValue &&
                        p.CatalogProductId.Value != Guid.Empty)
                    .GroupBy(p =>
                        p.CatalogProductId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Id);

            // ============================================================
            // PRODUCT RESULTS
            // ============================================================

            var productResults =
      products
          .Select(p =>
          {
              ProductCatalog? catalog = null;

              if (p.CatalogProductId.HasValue &&
                  p.CatalogProductId.Value != Guid.Empty)
              {
                  catalogMap.TryGetValue(
                      p.CatalogProductId.Value,
                      out catalog);
              }

              var isPack =
                  catalog?.IsPack ?? false;

              var packSize =
                  1m;

              Guid? componentProductId =
                  null;

              if (catalog?.IsPack == true &&
                  catalog.PackComponents.Any())
              {
                  var component =
                      catalog.PackComponents.First();

                  packSize =
                      component.Quantity > 0
                          ? component.Quantity
                          : 1m;

                  if (catalogToProductMap.TryGetValue(
                          component.ComponentCatalogId,
                          out var unitProductId))
                  {
                      componentProductId =
                          unitProductId;
                  }
              }

              return new ProductResult
              {
                  Id =
                      p.Id,

                  CatalogProductId =
                      p.CatalogProductId,

                  Name =
                      !string.IsNullOrWhiteSpace(p.Name)
                          ? p.Name
                          : catalog?.Name ??
                            "Unknown Product",

                  Sku =
                      !string.IsNullOrWhiteSpace(p.Sku)
                          ? p.Sku
                          : catalog?.InternalCode,

                  Barcode =
                      !string.IsNullOrWhiteSpace(p.Barcode)
                          ? p.Barcode
                          : catalog?.Barcode,

                  Description =
                      !string.IsNullOrWhiteSpace(p.Description)
                          ? p.Description
                          : catalog?.Description,

                  Category =
                      p.Category,

                  Brand =
                      !string.IsNullOrWhiteSpace(p.Brand)
                          ? p.Brand
                          : catalog?.Brand,

                  Unit =
                      !string.IsNullOrWhiteSpace(p.Unit)
                          ? p.Unit
                          : catalog?.UnitOfMeasure,

                  SalePrice =
                      p.SalePrice,

                  SalePrice2 =
                      p.SalePrice2,

                  SalePrice3 =
                      p.SalePrice3,

                  PurchasePrice =
                      p.PurchasePrice,

                  VatRate =
                      p.VatRate,

                  MinStockLevel =
                      p.MinStockLevel,

                  MaxStockLevel =
                      p.MaxStockLevel,

                  IsTracked =
                      p.IsTracked,

                  Status =
                      (Dto.Enums.ProductStatus)p.IsActive,

                  IsPack =
                      isPack,

                  PackSize =
                      packSize,

                  ComponentProductId =
                      componentProductId
              };
          })
          .ToList();

            // ============================================================
            // PRODUCT CATALOG RESULTS
            // ============================================================

            var catalogResults =
                productCatalogs
                    .Select(c =>
                    {
                        var result =
                            _mapper.Map<ProductCatalogResult>(c);

                        result.IsPack =
                            c.IsPack;

                        result.PackComponents =
                            c.PackComponents
                                .Select(pc =>
                                    new PackComponentResult
                                    {
                                        Id =
                                            pc.Id,

                                        ComponentCatalogId =
                                            pc.ComponentCatalogId,

                                        ComponentName =
                                            pc.ComponentCatalog?.Name ??
                                            string.Empty,

                                        ComponentBarCode =
                                            pc.ComponentCatalog?.Barcode,

                                        Quantity =
                                            pc.Quantity
                                    })
                                .ToList();

                        return result;
                    })
                    .ToList();

            // ============================================================
            // RESULT
            // ============================================================

            return new PosBootstrapResult
            {
                ServerTime =
                    DateTime.UtcNow,

                Products =
                    productResults,

                ProductCatalogs =
                    catalogResults,

                Stocks =
                    _mapper.Map<List<StockResult>>(
                        stocks),

                Customers =
                    _mapper.Map<List<CustomerResult>>(
                        customers),

                Suppliers =
                    _mapper.Map<List<SupplierResult>>(
                        suppliers),

                ProductCategories =
                    _mapper.Map<List<ProductCategoryResult>>(
                        productCategories),

                ActiveCashSession =
                    activeSession,

                Config =
                    new PosConfigResult
                    {
                        Currency =
                            "EUR",

                        DefaultVatRate =
                            21,

                        AllowNegativeStock =
                            false
                    }
            };
        }
    }
}