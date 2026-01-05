using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Promotions.Requests;
using Inventory.Dto.Promotions.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class PromotionService
    {
        private readonly IRepository<Promotion> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public PromotionService(
            IRepository<Promotion> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // CREATE
        public async Task<PromotionResult> CreateAsync(CreatePromotionRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            // FIXED: Changed != to == for proper tenant check
            var exists = await _repository.ExistsAsync(p =>
                p.Code == request.Code &&
                p.TenantId == tenantId &&
                !p.IsDeleted);

            if (exists)
            {
                throw new ConflictException(
                    $"Promotion with code '{request.Code}' already exists.");
            }

            if (request.StartDate >= request.EndDate)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "DateRange", new[] { "StartDate must be before EndDate." } }
                });
            }

            var entity = _mapper.Map<Promotion>(request);

            entity.Id = Guid.NewGuid();
            entity.TenantId = tenantId;
            entity.CreatedByUserId = userId;
            entity.IsActive = true;
            entity.CurrentUsageCount = 0;
            entity.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PromotionResult>(entity);
        }

        // GET BY ID
        public async Task<PromotionResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Promotion", id);

            return _mapper.Map<PromotionResult>(entity);
        }

        // GET ALL
        public async Task<List<PromotionResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var promotions = await _repository.GetAllAsync();

            return _mapper.Map<List<PromotionResult>>(
                promotions.Where(p => !p.IsDeleted && p.TenantId == tenantId).ToList()
            );
        }

        // UPDATE
        public async Task<PromotionResult> UpdateAsync(Guid id, UpdatePromotionRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Promotion", id);

            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != entity.Code)
            {
                var codeExists = await _repository.ExistsAsync(p =>
                    p.Code == request.Code &&
                    p.Id != id &&
                    p.TenantId == tenantId &&
                    !p.IsDeleted);

                if (codeExists)
                {
                    throw new ConflictException(
                        $"Promotion with code '{request.Code}' already exists.");
                }
            }

            if (request.StartDate >= request.EndDate)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "DateRange", new[] { "StartDate must be before EndDate." } }
                });
            }

            _mapper.Map(request, entity);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedByUserId = userId;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PromotionResult>(entity);
        }

        // ACTIVATE / DEACTIVATE
        public async Task<bool> SetActiveAsync(Guid id, bool isActive)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Promotion", id);

            entity.IsActive = isActive;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedByUserId = userId;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Promotion", id);

            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedByUserId = userId;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // QUERY
        public async Task<PagedResult<PromotionResult>> QueryAsync(PromotionQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var promotions = (await _repository.GetAllAsync())
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                promotions = promotions.Where(p =>
                    p.Name.Contains(query.Search) ||
                    p.Code.Contains(query.Search));
            }

            if (query.IsActive.HasValue)
                promotions = promotions.Where(p => p.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Type))
                promotions = promotions.Where(p => p.Type == query.Type);

            if (query.FromDate.HasValue)
                promotions = promotions.Where(p => p.StartDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                promotions = promotions.Where(p => p.EndDate <= query.ToDate.Value);

            promotions = query.SortBy.ToLower() switch
            {
                "name" => query.Desc ? promotions.OrderByDescending(p => p.Name) : promotions.OrderBy(p => p.Name),
                "code" => query.Desc ? promotions.OrderByDescending(p => p.Code) : promotions.OrderBy(p => p.Code),
                "startdate" => query.Desc ? promotions.OrderByDescending(p => p.StartDate) : promotions.OrderBy(p => p.StartDate),
                _ => query.Desc ? promotions.OrderByDescending(p => p.CreatedAt) : promotions.OrderBy(p => p.CreatedAt)
            };

            var total = promotions.Count();
            var items = promotions
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<PromotionResult>
            {
                Items = _mapper.Map<List<PromotionResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}