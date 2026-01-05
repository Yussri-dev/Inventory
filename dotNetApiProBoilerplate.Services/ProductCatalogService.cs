using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;
using Inventory.Services.Context;

namespace Inventory.Services
{
    public class ProductCatalogService
    {
        private readonly IRepository<ProductCatalog> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public ProductCatalogService(
            IRepository<ProductCatalog> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<ProductCatalogResult> CreateAsync(CreateProductCatalogRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Name", new[] { "ProductCatalog name must not be empty." } }
                });
            }

            var exists = await _repository.ExistsAsync(c =>
                c.Name == request.Name &&
                c.TenantId == tenantId &&
                !c.IsDeleted);

            if (exists)
            {
                throw new ConflictException(
                    $"ProductCatalog with name '{request.Name}' already exists.");
            }

            var customer = _mapper.Map<ProductCatalog>(request);

            customer.Id = Guid.NewGuid();
            customer.TenantId = tenantId;               // ✅ CRITICAL
            customer.CreatedAt = DateTime.UtcNow;
            customer.ModifiedAt = DateTime.UtcNow;
            customer.CreatedByUserId = userId;

            await _repository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductCatalogResult>(customer);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<ProductCatalogResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var customer = await _repository.GetByIdAsync(id);

            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
            {
                throw new NotFoundException("ProductCatalog", id);
            }

            return _mapper.Map<ProductCatalogResult>(customer);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<ProductCatalogResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var customers = await _repository.GetAllAsync();

            var activeProductCatalogs = customers
                .Where(c => !c.IsDeleted && c.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<ProductCatalogResult>>(activeProductCatalogs);
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<ProductCatalogResult> UpdateAsync(Guid id, UpdateProductCatalogRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var customer = await _repository.GetByIdAsync(id);

            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
            {
                throw new NotFoundException("ProductCatalog", id);
            }

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != customer.Name)
            {
                var nameExists = await _repository.ExistsAsync(c =>
                    c.Name == request.Name &&
                    c.Id != id &&
                    c.TenantId == tenantId &&
                    !c.IsDeleted);

                if (nameExists)
                {
                    throw new ConflictException(
                        $"ProductCatalog with name '{request.Name}' already exists.");
                }
            }

            _mapper.Map(request, customer);

            customer.ModifiedAt = DateTime.UtcNow;
            customer.ModifiedByUserId = userId;

            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductCatalogResult>(customer);
        }

        // =========================
        // DELETE (SOFT)
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var customer = await _repository.GetByIdAsync(id);

            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
            {
                throw new NotFoundException("ProductCatalog", id);
            }

            customer.IsDeleted = true;
            customer.DeletedAt = DateTime.UtcNow;
            customer.DeletedByUserId = userId;

            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<ProductCatalogResult>> QueryAsync(ProductCatalogQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var filtered = (await _repository.GetAllAsync())
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                filtered = filtered.Where(p =>
                    p.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
            }

            filtered = query.SortBy?.ToLower() switch
            {
                "name" => query.Desc
                    ? filtered.OrderByDescending(p => p.Name)
                    : filtered.OrderBy(p => p.Name),

                "manufacturer" => query.Desc
                    ? filtered.OrderByDescending(p => p.Manufacturer)
                    : filtered.OrderBy(p => p.Manufacturer),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<ProductCatalogResult>
            {
                Items = _mapper.Map<List<ProductCatalogResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
