using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.InventoryLines.Requests;
using Inventory.Dto.InventoryLines.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class InventoryLineService
    {
        private readonly IRepository<InventoryLine> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public InventoryLineService(
            IRepository<InventoryLine> repository,
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
        public async Task<InventoryLineResult> CreateAsync(CreateInventoryLineRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var exists = await _repository.ExistsAsync(l =>
                l.InventorySessionId == request.InventorySessionId &&
                l.ProductId == request.ProductId &&
                l.TenantId == tenantId &&
                !l.IsDeleted);

            if (exists)
                throw new ConflictException("Inventory line already exists for this product.");

            var line = _mapper.Map<InventoryLine>(request);

            line.Id = Guid.NewGuid();
            line.TenantId = tenantId;
            line.CreatedByUserId = userId;
            line.CountedAt = DateTime.UtcNow;
            line.IsAdjusted = false;
            line.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(line);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InventoryLineResult>(line);
        }

        // GET BY ID
        public async Task<InventoryLineResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var line = await _repository.GetByIdAsync(id);

            if (line == null || line.IsDeleted || line.TenantId != tenantId)
                throw new NotFoundException("InventoryLine", id);

            return _mapper.Map<InventoryLineResult>(line);
        }

        // GET ALL BY SESSION
        public async Task<List<InventoryLineResult>> GetBySessionAsync(Guid inventorySessionId)
        {
            var tenantId = _tenantContext.GetTenantId();
            var lines = (await _repository.GetAllAsync())
                .Where(l => !l.IsDeleted &&
                           l.TenantId == tenantId &&
                           l.InventorySessionId == inventorySessionId)
                .ToList();

            return _mapper.Map<List<InventoryLineResult>>(lines);
        }

        // GET ALL
        public async Task<List<InventoryLineResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var inventoryLines = await _repository.GetAllAsync();

            var activeLines = inventoryLines
                .Where(x => !x.IsDeleted && x.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<InventoryLineResult>>(activeLines);
        }

        // UPDATE (count only)
        public async Task<InventoryLineResult> UpdateAsync(Guid id, UpdateInventoryLineRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var line = await _repository.GetByIdAsync(id);

            if (line == null || line.IsDeleted || line.TenantId != tenantId)
                throw new NotFoundException("InventoryLine", id);

            if (line.IsAdjusted)
                throw new ConflictException("Adjusted inventory lines cannot be modified.");

            _mapper.Map(request, line);
            line.CountedAt = DateTime.UtcNow;
            line.ModifiedAt = DateTime.UtcNow;
            line.ModifiedByUserId = userId;

            _repository.Update(line);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InventoryLineResult>(line);
        }

        // MARK AS ADJUSTED
        public async Task<bool> MarkAsAdjustedAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var line = await _repository.GetByIdAsync(id);

            if (line == null || line.IsDeleted || line.TenantId != tenantId)
                throw new NotFoundException("InventoryLine", id);

            if (line.IsAdjusted)
                return true;

            line.IsAdjusted = true;
            line.AdjustedAt = DateTime.UtcNow;
            line.ModifiedAt = DateTime.UtcNow;
            line.ModifiedByUserId = userId;

            _repository.Update(line);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var line = await _repository.GetByIdAsync(id);

            if (line == null || line.IsDeleted || line.TenantId != tenantId)
                throw new NotFoundException("InventoryLine", id);

            if (line.IsAdjusted)
                throw new ConflictException("Adjusted inventory lines cannot be deleted.");

            line.IsDeleted = true;
            line.DeletedAt = DateTime.UtcNow;
            line.DeletedByUserId = userId;
            line.ModifiedAt = DateTime.UtcNow;

            _repository.Update(line);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // QUERY
        public async Task<PagedResult<InventoryLineResult>> QueryAsync(InventoryLineQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });

            var lines = (await _repository.GetAllAsync())
                .Where(l => !l.IsDeleted && l.TenantId == tenantId)
                .AsQueryable();

            if (query.InventorySessionId.HasValue)
                lines = lines.Where(l => l.InventorySessionId == query.InventorySessionId.Value);

            if (query.ProductId.HasValue)
                lines = lines.Where(l => l.ProductId == query.ProductId.Value);

            if (query.HasVariance.HasValue)
                lines = query.HasVariance.Value
                    ? lines.Where(l => l.Variance != 0)
                    : lines.Where(l => l.Variance == 0);

            lines = query.SortBy.ToLower() switch
            {
                "variance" => query.Desc ? lines.OrderByDescending(l => l.Variance) : lines.OrderBy(l => l.Variance),
                "countedat" => query.Desc ? lines.OrderByDescending(l => l.CountedAt) : lines.OrderBy(l => l.CountedAt),
                _ => query.Desc ? lines.OrderByDescending(l => l.CreatedAt) : lines.OrderBy(l => l.CreatedAt)
            };

            var total = lines.Count();
            var items = lines
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<InventoryLineResult>
            {
                Items = _mapper.Map<List<InventoryLineResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}