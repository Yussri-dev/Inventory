using AutoMapper;
using AutoMapper.QueryableExtensions;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.ProductCategory.Requests;
using Inventory.Dto.ProductCategory.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class ProductCategoryService
    {
        private readonly IRepository<ProductCategory> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public ProductCategoryService(
            IRepository<ProductCategory> repository,
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

        public async Task<ProductCategoryResult> CreateAsync(CreateProductCategoryRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Name", new[] { "Category name must not be empty." } }
                });
            }

            var exists = await _repository.ExistsAsync(c =>
                 c.Name == request.Name && !c.IsDeleted);

            if (exists)
                throw new ConflictException("Category already exists");

            var entity = _mapper.Map<ProductCategory>(request);

            entity.Id = Guid.NewGuid();
            entity.Name = request.Name.Trim();
            entity.Color = request.Color?.Trim();
            entity.Icon = request.Icon?.Trim();
            entity.DisplayOrder = request.DisplayOrder;
            entity.TenantId = tenantId;

            await _repository.AddAsync(entity);

            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ProductCategoryResult>(entity);
        }

        public async Task<ProductCategoryResult> GetByIdAsync(Guid id)
        {
            var entity = await _repository.Query()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (entity == null)
            {
                throw new NotFoundException("Category not found", id);
            }

            return _mapper.Map<ProductCategoryResult>(entity);

        }

        public async Task<List<ProductCategoryResult>> GetAllAsync()
        {
            var categories = await _repository.Query()
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            return _mapper.Map<List<ProductCategoryResult>>(categories);
        }

        public async Task<ProductCategoryResult> UpdateAsync(Guid id, UpdateProductCategoryRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var category = await _repository.Query()
                .FirstOrDefaultAsync(
                    c => c.Id == id && !c.IsDeleted &&
                    c.TenantId == tenantId
                );

            if (category == null)
                throw new NotFoundException("Category not found", id);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Name", new[] { "Category name must not be empty." } }
                });
            }


            _mapper.Map(request, category);

            category.ModifiedAt = DateTime.UtcNow;
            category.ModifiedByUserId = userId;

            _repository.Update(category);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductCategoryResult>(category);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var category = await _repository.Query()
                .FirstOrDefaultAsync(
                    c => c.Id == id
                    && !c.IsDeleted
                    && c.TenantId == tenantId
                );

            if (category == null)
                throw new NotFoundException("Category not found", id);

            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            category.DeletedByUserId = _tenantContext.UserId;

            _repository.Update(category);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<ProductCategoryResult>> QueryAsync(ProductCategoryQuery query)
        {
            if (query.Page < 1)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    {"Page", new[]{"Page must be >= 1"} }
                });
            }

            if (query.PageSize < 1 || query.PageSize > 1000)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    {"PageSize", new[]{"PageSize must be between 1 and 1000"} }
                });
            }

            var dbQuery = _repository.Query()
                .Where(x => !x.IsDeleted);

            // =========================
            // SEARCH
            // =========================
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                dbQuery = dbQuery.Where(p =>
                    EF.Functions.ILike(p.Name, $"%{search}%") ||
                    (p.Color != null && EF.Functions.ILike(p.Color, $"%{search}%"))
                );
            }

            // =========================
            // SORT (SAFE)
            // =========================
            var sortBy = query.SortBy?.ToLower();

            dbQuery = sortBy switch
            {
                "name" => query.Desc
                    ? dbQuery.OrderByDescending(p => p.Name)
                    : dbQuery.OrderBy(p => p.Name),

                "color" => query.Desc
                    ? dbQuery.OrderByDescending(p => p.Color)
                    : dbQuery.OrderBy(p => p.Color),

                _ => query.Desc
                    ? dbQuery.OrderByDescending(p => p.CreatedAt)
                    : dbQuery.OrderBy(p => p.CreatedAt),
            };

            // =========================
            // COUNT
            // =========================
            var total = await dbQuery.CountAsync();

            // =========================
            // DATA
            // =========================
            var items = await dbQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ProjectTo<ProductCategoryResult>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new PagedResult<ProductCategoryResult>
            {
                Items = items,
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize,
            };
        }

    }
}
