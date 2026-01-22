using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

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
        public async Task<ProductResult> CreateAsync(CreateProductRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var catalogProduct = await _catalogRepository.GetByIdAsync(request.CatalogProductId);
            if (catalogProduct == null || catalogProduct.IsDeleted)
                throw new NotFoundException("Product Catalog", request.CatalogProductId);

            var exists = await _repository.ExistsAsync(p =>
                p.CatalogProductId == request.CatalogProductId &&
                p.TenantId == tenantId &&
                !p.IsDeleted);

            if (exists)
                throw new ConflictException($"Product '{catalogProduct.Name}' already exists for this store.");

            if (request.PurchasePrice > request.SalePrice)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Price", new[] { "Purchase price cannot be greater than sale price." } }
                });

            var product = _mapper.Map<Product>(request);

            // ✅ REQUIRED SNAPSHOT
            product.Name = catalogProduct.Name;
            product.Barcode = catalogProduct.Barcode;
            product.Brand = catalogProduct.Brand;
            product.Description = catalogProduct.Description;

            product.Id = Guid.NewGuid();
            product.TenantId = tenantId;
            product.CreatedAt = DateTime.UtcNow;
            product.ModifiedAt = DateTime.UtcNow;
            product.CreatedByUserId = userId;

            await _repository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductResult>(product);
        }


        // GET BY ID
        public async Task<ProductResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();

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
            var tenantId = _tenantContext.GetTenantId();

            var products = await _repository.GetAllAsync();

            var activeProducts = products
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<ProductResult>>(activeProducts);
        }

        // UPDATE
        public async Task<ProductResult> UpdateAsync(Guid id, UpdateProductRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted || product.TenantId != tenantId)
            {
                throw new NotFoundException("Product", id);
            }

            // Validate prices
            if (request.SalePrice < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "SalePrice", new[] { "Sale price must be greater than or equal to 0." } }
                });
            }

            if (request.PurchasePrice < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PurchasePrice", new[] { "Purchase price must be greater than or equal to 0." } }
                });
            }

            if (request.PurchasePrice > request.SalePrice)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Price", new[] { "Purchase price cannot be greater than sale price" } }
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
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

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

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<ProductResult>> QueryAsync(ProductQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            // Validate query parameters
            if (query.Page < 1)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Page", new[] { "Page must be greater than or equal to 1." } }
                });
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                });
            }

            var allProducts = await _repository.GetAllAsync();
            var allCatalogs = await _catalogRepository.GetAllAsync();

            // Create a dictionary for fast catalog lookup
            var catalogDict = allCatalogs
                .Where(c => !c.IsDeleted)
                .ToDictionary(c => c.Id);

            // Filter tenant products - convert to in-memory list
            var filtered = allProducts
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .ToList(); // Convert to in-memory list here

            // Search filter - now working with in-memory data
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchTerm = query.Search.ToLower();

                filtered = filtered.Where(p =>
                {
                    // Get catalog product
                    if (!catalogDict.TryGetValue(p.CatalogProductId, out var catalog))
                        return false;

                    // Search in catalog fields - null-conditional operators work in memory
                    return (catalog.Name?.ToLower().Contains(searchTerm) ?? false) ||
                           (catalog.Barcode?.ToLower().Contains(searchTerm) ?? false) ||
                           (catalog.Description?.ToLower().Contains(searchTerm) ?? false) ||
                           (catalog.Brand?.ToLower().Contains(searchTerm) ?? false) ||
                           (catalog.Manufacturer?.ToLower().Contains(searchTerm) ?? false);
                }).ToList();
            }

            // Status filter
            if (query.Status.HasValue)
            {
                var status = (ProductStatus)query.Status.Value;
                filtered = filtered.Where(p => p.IsActive == status).ToList();
            }

            // Sorting (using catalog data where appropriate)
            IEnumerable<Product> sortedFiltered = query.SortBy?.ToLower() switch
            {
                "name" => query.Desc
                    ? filtered.OrderByDescending(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Name : "")
                    : filtered.OrderBy(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Name : ""),

                "barcode" => query.Desc
                    ? filtered.OrderByDescending(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Barcode : "")
                    : filtered.OrderBy(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Barcode : ""),

                "manufacturer" => query.Desc
                    ? filtered.OrderByDescending(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Manufacturer : "")
                    : filtered.OrderBy(p => catalogDict.ContainsKey(p.CatalogProductId) ? catalogDict[p.CatalogProductId].Manufacturer : ""),

                "saleprice" => query.Desc
                    ? filtered.OrderByDescending(p => p.SalePrice)
                    : filtered.OrderBy(p => p.SalePrice),

                "purchaseprice" => query.Desc
                    ? filtered.OrderByDescending(p => p.PurchasePrice)
                    : filtered.OrderBy(p => p.PurchasePrice),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = sortedFiltered.Count();

            var items = sortedFiltered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<ProductResult>
            {
                Items = _mapper.Map<List<ProductResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}