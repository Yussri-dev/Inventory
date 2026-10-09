using AutoMapper;
using AutoMapper.QueryableExtensions;
using Inventory.Domain.Entities;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class ProductService
    {
        private readonly IRepository<Product> _repository;
        private readonly IRepository<ProductCatalog> _catalogRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public ProductService(
            IRepository<Product> repository,
            IRepository<ProductCatalog> catalogRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext
            )
        {
            _repository = repository;
            _catalogRepository = catalogRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // CREATE
        public async Task<ProductResult> CreateAsync(
     CreateProductRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            if (tenantId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "No tenant is associated with the current session.");
            }

            ProductCatalog? catalogProduct = null;

            // ==========================================
            // CASE 1 : PRODUCT COMES FROM GLOBAL CATALOG
            // ==========================================

            if (request.CatalogProductId.HasValue &&
                request.CatalogProductId.Value != Guid.Empty)
            {
                catalogProduct =
                    await _catalogRepository.GetByIdAsync(
                        request.CatalogProductId.Value);

                if (catalogProduct == null ||
                    catalogProduct.IsDeleted)
                {
                    throw new NotFoundException(
                        "Product Catalog",
                        request.CatalogProductId.Value);
                }

                var exists =
                    await _repository.ExistsAsync(p =>
                        p.CatalogProductId ==
                            request.CatalogProductId.Value &&
                        p.TenantId == tenantId &&
                        !p.IsDeleted);

                if (exists)
                {
                    throw new ConflictException(
                        $"Product '{catalogProduct.Name}' " +
                        "already exists for this store.");
                }
            }

            // ==========================================
            // CASE 2 : CUSTOM PRODUCT CREATED BY TENANT
            // ==========================================

            else
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new ValidationException(
                        new Dictionary<string, string[]>
                        {
                    {
                        nameof(request.Name),
                        new[]
                        {
                            "Product name is required when no catalog product is selected."
                        }
                    }
                        });
                }

                if (!string.IsNullOrWhiteSpace(request.Barcode))
                {
                    var barcode =
                        request.Barcode.Trim();

                    var barcodeExists =
                        await _repository.ExistsAsync(p =>
                            p.TenantId == tenantId &&
                            p.Barcode == barcode &&
                            !p.IsDeleted);

                    if (barcodeExists)
                    {
                        throw new ConflictException(
                            "A product with the same barcode already exists for this store.");
                    }
                }
            }

            // ==========================================
            // PRICE VALIDATION
            // ==========================================

            if (request.PurchasePrice > request.SalePrice ||
                request.PurchasePrice > request.SalePrice2 ||
                request.PurchasePrice > request.SalePrice3)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                {
                    "Price",
                    new[]
                    {
                        "Purchase price cannot be greater than any sale price."
                    }
                }
                    });
            }

            if (request.MinStockLevel >
                request.MaxStockLevel)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                {
                    "Stock",
                    new[]
                    {
                        "Min stock cannot be greater than max stock."
                    }
                }
                    });
            }

            // ==========================================
            // CREATE PRODUCT
            // ==========================================

            var product =
                _mapper.Map<Product>(request);

            product.Id =
                Guid.NewGuid();

            product.TenantId =
                tenantId;

            product.CreatedAt =
                DateTime.UtcNow;

            product.ModifiedAt =
                DateTime.UtcNow;

            product.CreatedByUserId =
                userId;

            // ==========================================
            // CATALOG PRODUCT
            // ==========================================

            if (catalogProduct != null)
            {
                product.CatalogProductId =
                    catalogProduct.Id;

                product.Name =
                    catalogProduct.Name;

                product.Barcode =
                    catalogProduct.Barcode;

                product.Brand =
                    catalogProduct.Brand;

                product.Description =
                    catalogProduct.Description;

                product.Unit =
                    catalogProduct.UnitOfMeasure;
            }

            // ==========================================
            // CUSTOM TENANT PRODUCT
            // ==========================================

            else
            {
                product.CatalogProductId = null;

                product.Name =
                    request.Name!.Trim();

                product.Sku =
                    string.IsNullOrWhiteSpace(request.Sku)
                        ? null
                        : request.Sku.Trim();

                product.Barcode =
                    string.IsNullOrWhiteSpace(request.Barcode)
                        ? null
                        : request.Barcode.Trim();

                product.Brand =
                    string.IsNullOrWhiteSpace(request.Brand)
                        ? null
                        : request.Brand.Trim();

                product.Description =
                    string.IsNullOrWhiteSpace(
                        request.Description)
                        ? null
                        : request.Description.Trim();

                product.Category =
                    string.IsNullOrWhiteSpace(
                        request.Category)
                        ? null
                        : request.Category.Trim();

                product.Unit =
                    string.IsNullOrWhiteSpace(request.Unit)
                        ? null
                        : request.Unit.Trim();
            }

            await _repository.AddAsync(product);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductResult>(product);
        }


        // GET BY ID
        public async Task<ProductResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted || product.TenantId != tenantId)
            {
                throw new NotFoundException("Product", id);
            }

            return _mapper.Map<ProductResult>(product);
        }

        // GET ALL
        public async Task<List<ProductResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var products = await _repository.GetAllAsync();

            var activeProducts = products
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<ProductResult>>(activeProducts);
        }

        // UPDATE
        public async Task<ProductResult> UpdateAsync(Guid id, UpdateProductRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted || product.TenantId != tenantId)
            {
                throw new NotFoundException("Product", id);
            }

            // Validate prices
            if (request.SalePrice < 0 || request.SalePrice2 < 0 || request.SalePrice3 < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "SalePrice", new[] { "Sale prices must be >= 0." } }
                });
            }

            if (request.PurchasePrice < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PurchasePrice", new[] { "Purchase price must be greater than or equal to 0." } }
                });
            }

            if (request.PurchasePrice > request.SalePrice ||
                 request.PurchasePrice > request.SalePrice2 ||
                 request.PurchasePrice > request.SalePrice3)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                        {
                            { "Price", new[] { "Purchase price cannot be greater than any sale price." } }
                        });
            }

            if (request.MinStockLevel > request.MaxStockLevel)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Stock", new[] { "Min stock cannot be greater than max stock." } }
                });
            }
            _mapper.Map(request, product);

            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedByUserId = userId;

            _repository.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductResult>(product);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted || product.TenantId != tenantId)
            {
                throw new NotFoundException("Product", id);
            }

            product.IsDeleted = true;
            product.DeletedAt = DateTime.UtcNow;
            product.DeletedByUserId = userId;

            _repository.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<ProductResult> RequestCatalogApprovalAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            if (tenantId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "No tenant is associated with the current session.");
            }

            var product = await _repository.GetByIdAsync(id);

            if (product == null ||
                product.IsDeleted ||
                product.TenantId != tenantId)
            {
                throw new NotFoundException("Product", id);
            }

            // Seuls les produits personnalisés peuvent être proposés.
            if (product.CatalogProductId.HasValue &&
                product.CatalogProductId.Value != Guid.Empty)
            {
                throw new ValidationException(
                    "This product is already linked to the global catalog.");
            }

            if (product.CatalogApprovalStatus ==
                CatalogApprovalStatus.Approved)
            {
                throw new ValidationException(
                    "This product has already been approved.");
            }

            // Idempotent : s'il est déjà Pending, pas besoin d'échouer.
            if (product.CatalogApprovalStatus !=
                CatalogApprovalStatus.Pending)
            {
                product.CatalogApprovalStatus =
                    CatalogApprovalStatus.Pending;

                product.ModifiedAt =
                    DateTime.UtcNow;

                product.ModifiedByUserId =
                    userId;

                _repository.Update(product);

                await _unitOfWork.SaveChangesAsync();
            }

            return _mapper.Map<ProductResult>(product);
        }

        // PAGINATION + FILTERING + SORTING
        //public async Task<PagedResult<ProductResult>> QueryAsync(ProductQuery query)
        //{
        //    var tenantId = _tenantContext.TenantId;

        //    // Validate query parameters
        //    if (query.Page < 1)
        //    {
        //        throw new ValidationException(new Dictionary<string, string[]>
        //        {
        //            { "Page", new[] { "Page must be greater than or equal to 1." } }
        //        });
        //    }

        //    if (query.PageSize < 1 || query.PageSize > 100)
        //    {
        //        throw new ValidationException(new Dictionary<string, string[]>
        //        {
        //            { "PageSize", new[] { "PageSize must be between 1 and 100." } }
        //        });
        //    }

        //    var allProducts = await _repository.GetAllAsync();
        //    var allCatalogs = await _catalogRepository.GetAllAsync();

        //    // Create a dictionary for fast catalog lookup
        //    var catalogDict = allCatalogs
        //        .Where(c => !c.IsDeleted)
        //        .ToDictionary(c => c.Id);

        //    // Filter tenant products - convert to in-memory list
        //    var filtered = allProducts
        //        .Where(p => !p.IsDeleted && p.TenantId == tenantId)
        //        .ToList(); // Convert to in-memory list here

        //    // Search filter - now working with in-memory data
        //    if (!string.IsNullOrWhiteSpace(query.Search))
        //    {
        //        var searchTerm = query.Search.ToLower();

        //        filtered = filtered.Where(p =>
        //        {
        //            // Get catalog product
        //            if (!catalogDict.TryGetValue(p.CatalogProductId, out var catalog))
        //                return false;

        //            // Search in catalog fields - null-conditional operators work in memory
        //            return (catalog.Name?.ToLower().Contains(searchTerm) ?? false) ||
        //                   (catalog.Barcode?.ToLower().Contains(searchTerm) ?? false) ||
        //                   (catalog.Description?.ToLower().Contains(searchTerm) ?? false) ||
        //                   (catalog.Brand?.ToLower().Contains(searchTerm) ?? false) ||
        //                   (catalog.Manufacturer?.ToLower().Contains(searchTerm) ?? false);
        //        }).ToList();
        //    }

        //    // Status filter
        //    if (query.Status.HasValue)
        //    {
        //        var status = (ProductStatus)query.Status.Value;
        //        filtered = filtered.Where(p => p.IsActive == status).ToList();
        //    }

        //    // Sorting (using catalog data where appropriate)
        //    IEnumerable<Product> sortedFiltered = query.SortBy?.ToLower() switch
        //    {
        //        "name" => query.Desc
        //            ? filtered.OrderByDescending(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Name : "")
        //            : filtered.OrderBy(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Name : ""),

        //        "barcode" => query.Desc
        //            ? filtered.OrderByDescending(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Barcode : "")
        //            : filtered.OrderBy(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Barcode : ""),

        //        "manufacturer" => query.Desc
        //            ? filtered.OrderByDescending(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Manufacturer : "")
        //            : filtered.OrderBy(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Manufacturer : ""),

        //        "saleprice" => query.Desc
        //            ? filtered.OrderByDescending(p => p.SalePrice)
        //            : filtered.OrderBy(p => p.SalePrice),

        //        "purchaseprice" => query.Desc
        //            ? filtered.OrderByDescending(p => p.PurchasePrice)
        //            : filtered.OrderBy(p => p.PurchasePrice),

        //        _ => query.Desc
        //            ? filtered.OrderByDescending(p => p.CreatedAt)
        //            : filtered.OrderBy(p => p.CreatedAt)
        //    };

        //    var total = sortedFiltered.Count();

        //    var items = sortedFiltered
        //        .Skip((query.Page - 1) * query.PageSize)
        //        .Take(query.PageSize)
        //        .ToList();

        //    return new PagedResult<ProductResult>
        //    {
        //        Items = _mapper.Map<List<ProductResult>>(items),
        //        TotalCount = total,
        //        Page = query.Page,
        //        PageSize = query.PageSize
        //    };
        //}

        public async Task<PagedResult<ProductResult>> QueryAsync(
    ProductQuery query,
    CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var tenantId =
                _tenantContext.TenantId;

            if (tenantId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "No tenant is associated with the current session.");
            }

            if (query.Page < 1)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                {
                    nameof(query.Page),
                    new[]
                    {
                        "Page must be greater than or equal to 1."
                    }
                }
                    });
            }

            if (query.PageSize < 1 ||
                query.PageSize > 100)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                {
                    nameof(query.PageSize),
                    new[]
                    {
                        "PageSize must be between 1 and 100."
                    }
                }
                    });
            }

            var productsQuery =
                _repository.Query()
                    .AsNoTracking()
                    .Where(product =>
                        !product.IsDeleted &&
                        product.TenantId == tenantId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search =
                    query.Search.Trim();

                productsQuery =
                    productsQuery.Where(product =>

                        EF.Functions.ILike(
                            product.Name,
                            $"%{search}%") ||

                        (product.Barcode != null &&
                         EF.Functions.ILike(
                             product.Barcode,
                             $"%{search}%")) ||

                        (product.Brand != null &&
                         EF.Functions.ILike(
                             product.Brand,
                             $"%{search}%")) ||

                        (product.Sku != null &&
                         EF.Functions.ILike(
                             product.Sku,
                             $"%{search}%"))
                    );
            }

            var sortBy =
                query.SortBy?
                    .Trim()
                    .ToLowerInvariant()
                ?? "createdat";

            productsQuery =
                sortBy switch
                {
                    "name" =>
                        query.Desc
                            ? productsQuery.OrderByDescending(
                                product => product.Name)
                            : productsQuery.OrderBy(
                                product => product.Name),

                    "barcode" =>
                        query.Desc
                            ? productsQuery.OrderByDescending(
                                product => product.Barcode)
                            : productsQuery.OrderBy(
                                product => product.Barcode),

                    "saleprice" =>
                        query.Desc
                            ? productsQuery.OrderByDescending(
                                product => product.SalePrice)
                            : productsQuery.OrderBy(
                                product => product.SalePrice),

                    _ =>
                        query.Desc
                            ? productsQuery.OrderByDescending(
                                product => product.CreatedAt)
                            : productsQuery.OrderBy(
                                product => product.CreatedAt)
                };

            var total =
                await productsQuery.CountAsync(
                    cancellationToken);

            var items =
                await productsQuery
                    .Skip(
                        (query.Page - 1) *
                        query.PageSize)
                    .Take(query.PageSize)
                    .ProjectTo<ProductResult>(
                        _mapper.ConfigurationProvider)
                    .ToListAsync(
                        cancellationToken);

            return new PagedResult<ProductResult>
            {
                Items = items,
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<PagedResult<ProductResult>> QueryForAdminAsync(
     ProductQuery query)
        {
            if (query.Page < 1)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                {
                    "Page",
                    new[]
                    {
                        "Page must be greater than or equal to 1."
                    }
                }
                    });
            }

            if (query.PageSize < 1 ||
                query.PageSize > 100)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                {
                    "PageSize",
                    new[]
                    {
                        "PageSize must be between 1 and 100."
                    }
                }
                    });
            }

            var productsQuery =
                _repository.Query()
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search =
                    query.Search.Trim();

                productsQuery =
                    productsQuery.Where(p =>

                        EF.Functions.ILike(
                            p.Name,
                            $"%{search}%") ||

                        (p.Barcode != null &&
                         EF.Functions.ILike(
                             p.Barcode,
                             $"%{search}%")) ||

                        (p.Brand != null &&
                         EF.Functions.ILike(
                             p.Brand,
                             $"%{search}%")) ||

                        (p.Sku != null &&
                         EF.Functions.ILike(
                             p.Sku,
                             $"%{search}%"))
                    );
            }

            var sortBy =
                query.SortBy?
                    .Trim()
                    .ToLowerInvariant()
                ?? "createdat";

            productsQuery =
                sortBy switch
                {
                    "name" =>
                        query.Desc
                            ? productsQuery.OrderByDescending(
                                p => p.Name)
                            : productsQuery.OrderBy(
                                p => p.Name),

                    "barcode" =>
                        query.Desc
                            ? productsQuery.OrderByDescending(
                                p => p.Barcode)
                            : productsQuery.OrderBy(
                                p => p.Barcode),

                    "saleprice" =>
                        query.Desc
                            ? productsQuery.OrderByDescending(
                                p => p.SalePrice)
                            : productsQuery.OrderBy(
                                p => p.SalePrice),

                    _ =>
                        query.Desc
                            ? productsQuery.OrderByDescending(
                                p => p.CreatedAt)
                            : productsQuery.OrderBy(
                                p => p.CreatedAt)
                };

            var total =
                await productsQuery.CountAsync();

            var items =
                await productsQuery
                    .Skip(
                        (query.Page - 1) *
                        query.PageSize)
                    .Take(query.PageSize)
                    .ProjectTo<ProductResult>(
                        _mapper.ConfigurationProvider)
                    .ToListAsync();

            return new PagedResult<ProductResult>
            {
                Items = items,
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<List<ProductResult>> GetPendingCatalogApprovalsAsync(CancellationToken cancellationToken = default)
        {
            var products =
                await _repository.Query()
                    .AsNoTracking()
                    .Where(x =>
                        !x.IsDeleted &&
                        x.CatalogProductId == null &&
                        x.CatalogApprovalStatus ==
                            CatalogApprovalStatus.Pending)
                    .OrderBy(x => x.CreatedAt)
                    .ProjectTo<ProductResult>(
                        _mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

            return products;
        }
    }
}