using AutoMapper;
using Inventory.Domain.Enums;
using Inventory.Domain.Models;
using Inventory.Dto.InventoryLines.Requests;
using Inventory.Dto.InventoryLines.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class InventoryLineService
    {
        private readonly IRepository<InventoryLine> _repository;
        private readonly IRepository<InventorySession> _sessionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public InventoryLineService(
            IRepository<InventoryLine> repository,
            IRepository<InventorySession> sessionRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _sessionRepository = sessionRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // ================================
        // CREATE
        // ================================
        public async Task<InventoryLineResult> CreateAsync(CreateInventoryLineRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var session = await _sessionRepository.GetByIdAsync(request.InventorySessionId);

            if (session == null || session.IsDeleted || session.TenantId != tenantId)
                throw new NotFoundException("InventorySession", request.InventorySessionId);

            if (session.Status != InventoryStatus.InProgress)
                throw new ConflictException("Cannot add lines to a closed session.");

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
            line.CreatedAt = DateTime.UtcNow;
            line.IsAdjusted = false;

            await _repository.AddAsync(line);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InventoryLineResult>(line);
        }

        // ================================
        // UPDATE
        // ================================
        public async Task<InventoryLineResult> UpdateAsync(Guid id, UpdateInventoryLineRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var line = await _repository.GetByIdAsync(id);

            if (line == null || line.IsDeleted || line.TenantId != tenantId)
                throw new NotFoundException("InventoryLine", id);

            var session = await _sessionRepository.GetByIdAsync(line.InventorySessionId);

            if (session!.Status != InventoryStatus.InProgress)
                throw new ConflictException("Cannot modify lines of a closed session.");

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

        // ================================
        // DELETE
        // ================================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var line = await _repository.GetByIdAsync(id);

            if (line == null || line.IsDeleted || line.TenantId != tenantId)
                throw new NotFoundException("InventoryLine", id);

            var session = await _sessionRepository.GetByIdAsync(line.InventorySessionId);

            if (session!.Status != InventoryStatus.InProgress)
                throw new ConflictException("Cannot delete lines from a closed session.");

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


        // ================================
        // QUERY
        // ================================
        public async Task<PagedResult<InventoryLineResult>> QueryAsync(InventoryLineQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 200)
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Paging", new[] { "Invalid paging parameters." } }
        });

            var lines = _repository.Query()
                .Where(l => !l.IsDeleted && l.TenantId == tenantId);

            if (query.InventorySessionId.HasValue)
                lines = lines.Where(l => l.InventorySessionId == query.InventorySessionId.Value);

            if (query.ProductId.HasValue)
                lines = lines.Where(l => l.ProductId == query.ProductId.Value);

            if (query.HasVariance.HasValue)
                lines = query.HasVariance.Value
                    ? lines.Where(l => l.CountedQuantity - l.SystemQuantity != 0)
                    : lines.Where(l => l.CountedQuantity - l.SystemQuantity == 0);

            var sortBy = query.SortBy?.ToLower() ?? "createdat";

            lines = sortBy switch
            {
                "variance" => query.Desc
                    ? lines.OrderByDescending(l => l.CountedQuantity - l.SystemQuantity)
                    : lines.OrderBy(l => l.CountedQuantity - l.SystemQuantity),

                "countedat" => query.Desc
                    ? lines.OrderByDescending(l => l.CountedAt)
                    : lines.OrderBy(l => l.CountedAt),

                _ => query.Desc
                    ? lines.OrderByDescending(l => l.CreatedAt)
                    : lines.OrderBy(l => l.CreatedAt)
            };

            var total = await lines.CountAsync();

            var items = await lines
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

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