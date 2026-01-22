using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

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
        // CREATE (SuperAdmin only)
        // =========================
        public async Task<ProductCatalogResult> CreateAsync(CreateProductCatalogRequest request)
        {
            if (!_tenantContext.IsSuperAdmin)
                throw new ForbiddenException("Only system admin can create product catalogs.");

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Name", new[] { "ProductCatalog name must not be empty." } }
                });
            }

            var exists = await _repository.ExistsAsync(c =>
                c.Barcode == request.Barcode && !c.IsDeleted);

            if (exists)
                throw new ConflictException("ProductCatalog with same barcode already exists.");

            var entity = _mapper.Map<ProductCatalog>(request);

            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedByUserId = _tenantContext.UserId;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductCatalogResult>(entity);
        }

        // =========================
        // GET BY ID (GLOBAL)
        // =========================
        public async Task<ProductCatalogResult> GetByIdAsync(Guid id)
        {
            var catalog = await _repository.GetByIdAsync(id);

            if (catalog == null || catalog.IsDeleted)
                throw new NotFoundException("ProductCatalog", id);

            return _mapper.Map<ProductCatalogResult>(catalog);
        }

        // =========================
        // GET ALL (GLOBAL)
        // =========================
        public async Task<List<ProductCatalogResult>> GetAllAsync()
        {
            var catalogs = await _repository.GetAllAsync();

            return _mapper.Map<List<ProductCatalogResult>>(
                catalogs.Where(c => !c.IsDeleted)
            );
        }

        // =========================
        // UPDATE (SuperAdmin only)
        // =========================
        public async Task<ProductCatalogResult> UpdateAsync(Guid id, UpdateProductCatalogRequest request)
        {
            if (!_tenantContext.IsSuperAdmin)
                throw new ForbiddenException("Only system admin can update product catalogs.");

            var catalog = await _repository.GetByIdAsync(id);

            if (catalog == null || catalog.IsDeleted)
                throw new NotFoundException("ProductCatalog", id);

            if (!string.IsNullOrWhiteSpace(request.Barcode) &&
                request.Barcode != catalog.Barcode)
            {
                var barcodeExists = await _repository.ExistsAsync(c =>
                    c.Barcode == request.Barcode &&
                    c.Id != id &&
                    !c.IsDeleted);

                if (barcodeExists)
                    throw new ConflictException("Another ProductCatalog uses this barcode.");
            }

            _mapper.Map(request, catalog);

            catalog.ModifiedAt = DateTime.UtcNow;
            catalog.ModifiedByUserId = _tenantContext.UserId;

            _repository.Update(catalog);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductCatalogResult>(catalog);
        }

        // =========================
        // DELETE (SuperAdmin only)
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            if (!_tenantContext.IsSuperAdmin)
                throw new ForbiddenException("Only system admin can delete product catalogs.");

            var catalog = await _repository.GetByIdAsync(id);

            if (catalog == null || catalog.IsDeleted)
                throw new NotFoundException("ProductCatalog", id);

            catalog.IsDeleted = true;
            catalog.DeletedAt = DateTime.UtcNow;
            catalog.DeletedByUserId = _tenantContext.UserId;

            _repository.Update(catalog);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY (GLOBAL)
        // =========================
        public async Task<PagedResult<ProductCatalogResult>> QueryAsync(ProductCatalogQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var filtered = (await _repository.GetAllAsync())
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                filtered = filtered.Where(p =>
                    p.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    p.Barcode.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
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
