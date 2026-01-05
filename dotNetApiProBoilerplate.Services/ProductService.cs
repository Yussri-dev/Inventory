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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public ProductService(
            IRepository<Product> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext
            )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // CREATE
        public async Task<ProductResult> CreateAsync(CreateProductRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            // Validation: Check if product with same name already exists
            var exists = await _repository.ExistsAsync(p => 
            p.Name == request.Name &&
            p.TenantId == tenantId &&
            !p.IsDeleted);
            if (exists)
            {
                throw new ConflictException($"Product with name '{request.Name}' already exists.");
            }

            // Validation: Check price
            if (request.SalePrice <= 0)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "SalePrice", new[] { "Sale price must be greater than or equal to 0." } }
                };
                throw new ValidationException(errors);
            }

            if (request.PurchasePrice <= 0)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "PurchasePrice", new[] { "Purchase price must be greater than or equal to 0." } }
                };
                throw new ValidationException(errors);
            }

            if (request.PurchasePrice > request.SalePrice)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Price", new[] { "Purchase price must be greater than sale price" } }
                };
                throw new ValidationException(errors);
            }

            var product = _mapper.Map<Product>(request);

            product.Id = Guid.NewGuid();
            product.IsActive = ProductStatus.Active;
            product.CreatedAt = DateTime.UtcNow;
            product.ModifiedAt = DateTime.UtcNow;
            product.TenantId = tenantId;
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

            // Filter out soft-deleted products
            var activeProducts = products.Where(p => 
            !p.IsDeleted &&
            p.TenantId == tenantId).ToList();

            return _mapper.Map<List<ProductResult>>(activeProducts);
        }

        // UPDATE
        public async Task<ProductResult> UpdateAsync(Guid id, UpdateProductRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted || product.TenantId == tenantId)
            {
                throw new NotFoundException("Product", id);
            }

            // Check if trying to update to a name that already exists
            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != product.Name)
            {
                var nameExists = await _repository.ExistsAsync(p =>
                    p.Name == request.Name && p.Id != id && !p.IsDeleted);

                if (nameExists)
                {
                    throw new ConflictException($"Product with name '{request.Name}' already exists.");
                }
            }

            // Validation: Check price
            if (request.SalePrice < 0)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "SalePrice", new[] { "Sale price must be greater than or equal to 0." } }
                };
                throw new ValidationException(errors);
            }

            if (request.PurchasePrice < 0)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "PurchasePrice", new[] { "Purchase price must be greater than or equal to 0." } }
                };
                throw new ValidationException(errors);
            }

            if (request.PurchasePrice < request.SalePrice)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Price", new[] { "Purchase price must be greater than sale price" } }
                };
                throw new ValidationException(errors);
            }

            // Map the request to the product
            _mapper.Map(request, product);

            // Always update the ModifiedAt timestamp
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
            // Validate query parameters
            if (query.Page < 1)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Page", new[] { "Page must be greater than or equal to 1." } }
                };
                throw new ValidationException(errors);
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                };
                throw new ValidationException(errors);
            }

            var all = await _repository.GetAllAsync();

            // Filter out soft-deleted products
            var filtered = all.Where(p => !p.IsDeleted).AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                filtered = filtered.Where(p =>
                    p.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description != null && p.Description.Contains(query.Search, StringComparison.OrdinalIgnoreCase)));
            }

            // Status filter
            if (query.Status.HasValue)
            {
                var status = (ProductStatus)query.Status.Value;
                filtered = filtered.Where(p => p.IsActive == status);
            }

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "name" => query.Desc
                    ? filtered.OrderByDescending(p => p.Name)
                    : filtered.OrderBy(p => p.Name),

                "salePrice" => query.Desc
                    ? filtered.OrderByDescending(p => p.SalePrice)
                    : filtered.OrderBy(p => p.SalePrice),

                "purchasePrice" => query.Desc
                    ? filtered.OrderByDescending(p => p.PurchasePrice)
                    : filtered.OrderBy(p => p.PurchasePrice),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
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