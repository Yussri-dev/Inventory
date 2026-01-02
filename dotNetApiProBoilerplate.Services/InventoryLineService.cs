using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.InventoryLines.Requests;
using Inventory.Dto.InventoryLines.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class InventoryLineService
    {
        private readonly IRepository<InventoryLine> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InventoryLineService(
            IRepository<InventoryLine> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<InventoryLineResult> CreateAsync(CreateInventoryLineRequest request)
        {
            var exists = await _repository.ExistsAsync(l =>
                l.InventorySessionId == request.InventorySessionId &&
                l.ProductId == request.ProductId &&
                !l.IsDeleted);

            if (exists)
                throw new ConflictException("Inventory line already exists for this product.");

            var line = _mapper.Map<InventoryLine>(request);

            line.Id = Guid.NewGuid();
            line.CountedAt = DateTime.UtcNow;
            line.IsAdjusted = false;
            line.CreatedAt = DateTime.UtcNow;
            line.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(line);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InventoryLineResult>(line);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<InventoryLineResult> GetByIdAsync(Guid id)
        {
            var line = await _repository.GetByIdAsync(id);

            if (line == null || line.IsDeleted)
                throw new NotFoundException("InventoryLine", id);

            return _mapper.Map<InventoryLineResult>(line);
        }

        // =========================
        // GET ALL BY SESSION
        // =========================
        public async Task<List<InventoryLineResult>> GetBySessionAsync(Guid inventorySessionId)
        {
            var lines = (await _repository.GetAllAsync())
                .Where(l => !l.IsDeleted && l.InventorySessionId == inventorySessionId)
                .ToList();

            return _mapper.Map<List<InventoryLineResult>>(lines);
        }

        // =========================
        // GET All
        // =========================

        public async Task<List<InventoryLineResult>> GetAllAsync()
        {
            var inventoryLines = await _repository.GetAllAsync();

            var activeLines = inventoryLines.Where(x => x!.IsDeleted).ToList();

            return _mapper.Map<List<InventoryLineResult>>(activeLines);
        }
        // =========================
        // UPDATE (count only)
        // =========================
        public async Task<InventoryLineResult> UpdateAsync(Guid id, UpdateInventoryLineRequest request)
        {
            var line = await _repository.GetByIdAsync(id);

            if (line == null || line.IsDeleted)
                throw new NotFoundException("InventoryLine", id);

            if (line.IsAdjusted)
                throw new ConflictException("Adjusted inventory lines cannot be modified.");

            _mapper.Map(request, line);

            line.CountedAt = DateTime.UtcNow;
            line.ModifiedAt = DateTime.UtcNow;

            _repository.Update(line);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InventoryLineResult>(line);
        }

        // =========================
        // MARK AS ADJUSTED
        // =========================
        public async Task<bool> MarkAsAdjustedAsync(Guid id)
        {
            var line = await _repository.GetByIdAsync(id);

            if (line == null || line.IsDeleted)
                throw new NotFoundException("InventoryLine", id);

            if (line.IsAdjusted)
                return true;

            line.IsAdjusted = true;
            line.AdjustedAt = DateTime.UtcNow;
            line.ModifiedAt = DateTime.UtcNow;

            _repository.Update(line);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // SOFT DELETE (rare, admin only)
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var line = await _repository.GetByIdAsync(id);

            if (line == null || line.IsDeleted)
                throw new NotFoundException("InventoryLine", id);

            if (line.IsAdjusted)
                throw new ConflictException("Adjusted inventory lines cannot be deleted.");

            line.IsDeleted = true;
            line.ModifiedAt = DateTime.UtcNow;

            _repository.Update(line);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<InventoryLineResult>> QueryAsync(InventoryLineQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });

            var lines = (await _repository.GetAllAsync())
                .Where(l => !l.IsDeleted)
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
                "variance" => query.Desc
                    ? lines.OrderByDescending(l => l.Variance)
                    : lines.OrderBy(l => l.Variance),

                "countedat" => query.Desc
                    ? lines.OrderByDescending(l => l.CountedAt)
                    : lines.OrderBy(l => l.CountedAt),

                _ => query.Desc
                    ? lines.OrderByDescending(l => l.CreatedAt)
                    : lines.OrderBy(l => l.CreatedAt)
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
