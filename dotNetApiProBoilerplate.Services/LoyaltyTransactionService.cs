using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.LoyaltyTransactions.Requests;
using Inventory.Dto.LoyaltyTransactions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class LoyaltyTransactionService
    {
        private readonly IRepository<LoyaltyTransaction> _transactionRepository;
        private readonly IRepository<LoyaltyCard> _cardRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;
        public LoyaltyTransactionService(
            IRepository<LoyaltyTransaction> transactionRepository,
            IRepository<LoyaltyCard> cardRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext
            )
        {
            _transactionRepository = transactionRepository;
            _cardRepository = cardRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<LoyaltyTransactionResult> CreateAsync(CreateLoyaltyTransactionRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            if (request.PointsChange == 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PointsChange", new[] { "PointsChange cannot be zero." } }
                });
            }

            var card = await _cardRepository.GetByIdAsync(request.LoyaltyCardId);

            if (card == null || card.IsDeleted || !card.IsActive || card.TenantId != tenantId)
                throw new NotFoundException("LoyaltyCard", request.LoyaltyCardId);

            var pointsBefore = card.CurrentPoints;
            var pointsAfter = pointsBefore + request.PointsChange;

            if (pointsAfter < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PointsChange", new[] { "Resulting points cannot be negative." } }
                });
            }

            var entity = _mapper.Map<LoyaltyTransaction>(request);

            entity.Id = Guid.NewGuid();
            entity.PointsBefore = pointsBefore;
            entity.PointsAfter = pointsAfter;
            entity.TransactionDate = DateTime.UtcNow;
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.TenantId = tenantId;
            entity.CreatedByUserId = userId;
            // Apply loyalty card update
            card.CurrentPoints = pointsAfter;

            if (request.PointsChange > 0)
                card.LifetimePoints += request.PointsChange;

            card.ModifiedAt = DateTime.UtcNow;

            await _transactionRepository.AddAsync(entity);
            _cardRepository.Update(card);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<LoyaltyTransactionResult>(entity);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<LoyaltyTransactionResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var entity = await _transactionRepository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("LoyaltyTransaction", id);

            return _mapper.Map<LoyaltyTransactionResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<LoyaltyTransactionResult>> GetAllAsync()
        {
            var items = await _transactionRepository.GetAllAsync();

            return _mapper.Map<List<LoyaltyTransactionResult>>(
                items.Where(t => !t.IsDeleted).ToList()
            );
        }

        // =========================
        // DELETE (audit-safe)
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var entity = await _transactionRepository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("LoyaltyTransaction", id);

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.DeletedByUserId = userId;

            _transactionRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<LoyaltyTransactionResult>> QueryAsync(LoyaltyTransactionQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var transactions = (await _transactionRepository.GetAllAsync())
                .Where(t => !t.IsDeleted)
                .AsQueryable();

            if (query.LoyaltyCardId.HasValue)
                transactions = transactions.Where(t => t.LoyaltyCardId == query.LoyaltyCardId.Value);

            if (query.SaleId.HasValue)
                transactions = transactions.Where(t => t.SaleId == query.SaleId.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
                transactions = transactions.Where(t =>
                    t.Reason.Contains(query.Search));

            transactions = query.SortBy.ToLower() switch
            {
                "pointschange" => query.Desc
                    ? transactions.OrderByDescending(t => t.PointsChange)
                    : transactions.OrderBy(t => t.PointsChange),

                "transactiondate" => query.Desc
                    ? transactions.OrderByDescending(t => t.TransactionDate)
                    : transactions.OrderBy(t => t.TransactionDate),

                _ => query.Desc
                    ? transactions.OrderByDescending(t => t.CreatedAt)
                    : transactions.OrderBy(t => t.CreatedAt)
            };

            var total = transactions.Count();

            var items = transactions
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<LoyaltyTransactionResult>
            {
                Items = _mapper.Map<List<LoyaltyTransactionResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
