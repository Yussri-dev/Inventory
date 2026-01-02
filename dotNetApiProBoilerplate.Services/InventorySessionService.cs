using AutoMapper;
using Inventory.Domain.Enums;
using Inventory.Domain.Models;
using Inventory.Dto.Enums;
using Inventory.Dto.InventorySessions.Requests;
using Inventory.Dto.InventorySessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class InventorySessionService
    {
        private readonly IRepository<InventorySession> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InventorySessionService(
            IRepository<InventorySession> repository,
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
        public async Task<InventorySessionResult> CreateAsync(CreateInventorySessionRequest request)
        {
            var exists = await _repository.ExistsAsync(s =>
                s.SessionNumber == request.SessionNumber && !s.IsDeleted);

            if (exists)
                throw new ConflictException(
                    $"Inventory session '{request.SessionNumber}' already exists.");

            var entity = _mapper.Map<InventorySession>(request);

            entity.Id = Guid.NewGuid();
            entity.Status = Domain.Enums.InventoryStatus.InProgress;
            entity.StartedAt = DateTime.UtcNow;
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InventorySessionResult>(entity);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<InventorySessionResult> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("InventorySession", id);

            return _mapper.Map<InventorySessionResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<InventorySessionResult>> GetAllAsync()
        {
            var sessions = await _repository.GetAllAsync();

            return _mapper.Map<List<InventorySessionResult>>(
                sessions.Where(s => !s.IsDeleted).ToList());
        }

        // =========================
        // UPDATE (metadata only)
        // =========================
        public async Task<InventorySessionResult> UpdateAsync(Guid id, UpdateInventorySessionRequest request)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("InventorySession", id);

            _mapper.Map(request, entity);
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InventorySessionResult>(entity);
        }

        // =========================
        // CLOSE
        // =========================
        public async Task<bool> CloseAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("InventorySession", id);

            if (entity.Status != (Domain.Enums.InventoryStatus)Dto.Enums.InventoryStatus.InProgress)
                throw new ConflictException("Inventory session is not open.");

            entity.Status =(Domain.Enums.InventoryStatus) Dto.Enums.InventoryStatus.Completed;
            entity.ClosedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // VALIDATE
        // =========================
        public async Task<bool> ValidateAsync(Guid id, Guid validatedByUserId)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("InventorySession", id);

            if (entity.Status != (Domain.Enums.InventoryStatus)Dto.Enums.InventoryStatus.Completed)
                throw new ConflictException("Inventory session must be closed before validation.");

            entity.Status = (Domain.Enums.InventoryStatus)Dto.Enums.InventoryStatus.Validated;
            entity.ValidatedAt = DateTime.UtcNow;
            entity.ValidatedByUserId = validatedByUserId;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // DELETE (soft)
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("InventorySession", id);

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<InventorySessionResult>> QueryAsync(InventorySessionQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });

            var sessions = (await _repository.GetAllAsync())
                .Where(s => !s.IsDeleted)
                .AsQueryable();

            if (query.Status.HasValue)
                sessions = sessions.Where(s => s.Status ==(Domain.Enums.InventoryStatus) query.Status.Value);

            if (query.UserId.HasValue)
                sessions = sessions.Where(s => s.UserId == query.UserId.Value);

            if (query.FromDate.HasValue)
                sessions = sessions.Where(s => s.StartedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                sessions = sessions.Where(s => s.StartedAt <= query.ToDate.Value);

            sessions = query.SortBy.ToLower() switch
            {
                "sessionnumber" => query.Desc
                    ? sessions.OrderByDescending(s => s.SessionNumber)
                    : sessions.OrderBy(s => s.SessionNumber),

                "status" => query.Desc
                    ? sessions.OrderByDescending(s => s.Status)
                    : sessions.OrderBy(s => s.Status),

                _ => query.Desc
                    ? sessions.OrderByDescending(s => s.StartedAt)
                    : sessions.OrderBy(s => s.StartedAt)
            };

            var total = sessions.Count();

            var items = sessions
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<InventorySessionResult>
            {
                Items = _mapper.Map<List<InventorySessionResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
