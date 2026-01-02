using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.LoyaltyCards.Requests;
using Inventory.Dto.LoyaltyCards.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class LoyaltyCardService
    {
        private readonly IRepository<LoyaltyCard> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public LoyaltyCardService(
            IRepository<LoyaltyCard> repository,
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
        public async Task<LoyaltyCardResult> CreateAsync(CreateLoyaltyCardRequest request)
        {
            var exists = await _repository.ExistsAsync(c =>
                c.CardNumber == request.CardNumber && !c.IsDeleted);

            if (exists)
                throw new ConflictException(
                    $"Loyalty card '{request.CardNumber}' already exists.");

            var entity = _mapper.Map<LoyaltyCard>(request);

            entity.Id = Guid.NewGuid();
            entity.CurrentPoints = 0;
            entity.LifetimePoints = 0;
            entity.IsActive = true;
            entity.IssuedAt = DateTime.UtcNow;
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<LoyaltyCardResult>(entity);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<LoyaltyCardResult> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("LoyaltyCard", id);

            return _mapper.Map<LoyaltyCardResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<LoyaltyCardResult>> GetAllAsync()
        {
            var cards = await _repository.GetAllAsync();

            return _mapper.Map<List<LoyaltyCardResult>>(
                cards.Where(c => !c.IsDeleted).ToList());
        }

        // =========================
        // UPDATE (metadata only)
        // =========================
        public async Task<LoyaltyCardResult> UpdateAsync(Guid id, UpdateLoyaltyCardRequest request)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("LoyaltyCard", id);

            _mapper.Map(request, entity);
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<LoyaltyCardResult>(entity);
        }

        // =========================
        // ADD POINTS
        // =========================
        public async Task<bool> AddPointsAsync(Guid id, int points)
        {
            if (points <= 0)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Points", new[] { "Points must be greater than 0." } }
                });

            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || !entity.IsActive)
                throw new NotFoundException("LoyaltyCard", id);

            entity.CurrentPoints += points;
            entity.LifetimePoints += points;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // DEACTIVATE
        // =========================
        public async Task<bool> DeactivateAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("LoyaltyCard", id);

            entity.IsActive = false;
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
                throw new NotFoundException("LoyaltyCard", id);

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<LoyaltyCardResult>> QueryAsync(LoyaltyCardQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });

            var cards = (await _repository.GetAllAsync())
                .Where(c => !c.IsDeleted)
                .AsQueryable();

            if (query.CustomerId.HasValue)
                cards = cards.Where(c => c.CustomerId == query.CustomerId.Value);

            if (query.IsActive.HasValue)
                cards = cards.Where(c => c.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
                cards = cards.Where(c =>
                    c.CardNumber.Contains(query.Search));

            cards = query.SortBy.ToLower() switch
            {
                "cardnumber" => query.Desc
                    ? cards.OrderByDescending(c => c.CardNumber)
                    : cards.OrderBy(c => c.CardNumber),

                "points" => query.Desc
                    ? cards.OrderByDescending(c => c.CurrentPoints)
                    : cards.OrderBy(c => c.CurrentPoints),

                _ => query.Desc
                    ? cards.OrderByDescending(c => c.IssuedAt)
                    : cards.OrderBy(c => c.IssuedAt)
            };

            var total = cards.Count();

            var items = cards
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<LoyaltyCardResult>
            {
                Items = _mapper.Map<List<LoyaltyCardResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
