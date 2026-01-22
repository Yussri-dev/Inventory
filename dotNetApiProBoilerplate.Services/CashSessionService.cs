using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CashSessionService : ICashSessionService
    {
        private readonly IRepository<CashSession> _repository;
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<CashMovement> _cashMovementRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public CashSessionService(
            IRepository<CashSession> repository,
            IRepository<Sale> saleRepository,
            IRepository<CashMovement> cashMovementRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _saleRepository = saleRepository;
            _cashMovementRepository = cashMovementRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // =========================
        // GET ACTIVE SESSION
        // =========================
        public async Task<CashSessionResult?> GetActiveSessionAsync()
        {
            var tenantId = _tenantContext.GetTenantId();

            var activeSession = await _repository.GetSingleAsync(
                cs => cs.TenantId == tenantId &&
                      cs.Status == CashSessionStatus.Open &&
                      !cs.IsDeleted);

            if (activeSession == null)
                return null;

            return _mapper.Map<CashSessionResult>(activeSession);
        }

        // =========================
        // ENSURE ACTIVE SESSION EXISTS
        // =========================
        public async Task<Guid> EnsureActiveSessionAsync()
        {
            var activeSession = await GetActiveSessionAsync();

            if (activeSession == null)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "CashSession", new[] { "No active cash session. Please open a cash session before making sales." } }
                });
            }

            return activeSession.Id;
        }

        // =========================
        // CREATE (OPEN SESSION)
        // =========================
        public async Task<CashSessionResult> CreateAsync(CreateCashSessionRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            // Vérifier qu'il n'y a pas déjà une session ouverte
            var existingOpenSession = await _repository.GetSingleAsync(
                cs => cs.TenantId == tenantId &&
                      cs.Status == CashSessionStatus.Open &&
                      !cs.IsDeleted);

            if (existingOpenSession != null)
            {
                throw new ConflictException(
                    $"A cash session is already open (Session #{existingOpenSession.SessionNumber}). Close it before opening a new one.");
            }

            var cashSession = _mapper.Map<CashSession>(request);

            cashSession.Id = Guid.NewGuid();
            cashSession.TenantId = tenantId;
            cashSession.CreatedByUserId = userId;
            cashSession.OpenedByUserId = userId;
            cashSession.OpenedAt = DateTime.UtcNow;
            cashSession.Status = CashSessionStatus.Open;
            cashSession.OpeningAmount = request.OpeningAmount;
            cashSession.ClosingAmountExpected = request.OpeningAmount;
            cashSession.CreatedAt = DateTime.UtcNow;
            cashSession.ModifiedAt = DateTime.UtcNow;

            // Générer le numéro de session
            var sessionCount = (await _repository.GetAllAsync())
                .Count(s => s.TenantId == tenantId);
            cashSession.SessionNumber = $"CS-{DateTime.UtcNow:yyyyMMdd}-{(sessionCount + 1):D4}";

            await _repository.AddAsync(cashSession);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashSessionResult>(cashSession);
        }

        // =========================
        // CLOSE SESSION
        // =========================
        public async Task<CashSessionResult> CloseSessionAsync(Guid id, CloseCashSessionRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null || cashSession.IsDeleted || cashSession.TenantId != tenantId)
            {
                throw new NotFoundException("CashSession", id);
            }

            if (cashSession.Status != CashSessionStatus.Open)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Status", new[] { "Cash session is not open." } }
                });
            }

            // Calculer les totaux de la session
            var sales = (await _saleRepository.GetAllAsync())
                .Where(s => s.CashSessionId == id && !s.IsDeleted)
                .ToList();

            var cashMovements = (await _cashMovementRepository.GetAllAsync())
                .Where(cm => cm.CashSessionId == id && !cm.IsDeleted)
                .ToList();

            // Calculer le cash attendu
            var salesTotal = sales.Sum(s => s.PaidAmount);
            var cashIn = cashMovements.Where(cm => cm.Type == CashMovementType.Sale).Sum(cm => cm.Amount);
            var cashOut = cashMovements.Where(cm => cm.Type == CashMovementType.Refund).Sum(cm => cm.Amount);

            cashSession.ClosingAmountExpected = cashSession.OpeningAmount + salesTotal + cashIn - cashOut;
            cashSession.ClosingAmountCounted = request.ActualCash;
            cashSession.Difference = request.ActualCash - cashSession.ClosingAmountExpected;
            cashSession.ClosingNotes = request.ClosingNotes;
            cashSession.Status = CashSessionStatus.Closed;
            cashSession.ClosedAt = DateTime.UtcNow;
            cashSession.ClosedByUserId = userId;
            cashSession.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashSession);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashSessionResult>(cashSession);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<CashSessionResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null || cashSession.IsDeleted || cashSession.TenantId != tenantId)
            {
                throw new NotFoundException("CashSession", id);
            }

            return _mapper.Map<CashSessionResult>(cashSession);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<CashSessionResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var cashSessions = await _repository.GetAllAsync();

            var activeCashSessions = cashSessions
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<CashSessionResult>>(activeCashSessions);
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<CashSessionResult> UpdateAsync(Guid id, UpdateCashSessionRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null || cashSession.IsDeleted || cashSession.TenantId != tenantId)
            {
                throw new NotFoundException("CashSession", id);
            }

            if (cashSession.Status == CashSessionStatus.Closed)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Status", new[] { "Cannot update a closed cash session." } }
                });
            }

            _mapper.Map(request, cashSession);
            cashSession.ModifiedAt = DateTime.UtcNow;
            cashSession.ModifiedByUserId = userId;

            _repository.Update(cashSession);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashSessionResult>(cashSession);
        }

        // =========================
        // SOFT DELETE
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null || cashSession.IsDeleted || cashSession.TenantId != tenantId)
            {
                throw new NotFoundException("CashSession", id);
            }

            if (cashSession.Status == CashSessionStatus.Open)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Status", new[] { "Cannot delete an open cash session. Close it first." } }
                });
            }

            cashSession.IsDeleted = true;
            cashSession.DeletedAt = DateTime.UtcNow;
            cashSession.DeletedByUserId = userId;
            cashSession.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashSession);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<CashSessionResult>> QueryAsync(CashSessionQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });

            var sessions = (await _repository.GetAllAsync())
                .Where(s => !s.IsDeleted && s.TenantId == tenantId)
                .AsQueryable();

            // FILTERS
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                sessions = sessions.Where(s =>
                    s.SessionNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    (s.OpeningNotes != null && s.OpeningNotes.Contains(query.Search, StringComparison.OrdinalIgnoreCase)) ||
                    (s.ClosingNotes != null && s.ClosingNotes.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (query.Status.HasValue)
                sessions = sessions.Where(s => s.Status == (CashSessionStatus)query.Status.Value);

            if (query.OpenedByUserId.HasValue)
                sessions = sessions.Where(s => s.OpenedByUserId == query.OpenedByUserId.Value);

            if (query.FromDate.HasValue)
                sessions = sessions.Where(s => s.OpenedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                sessions = sessions.Where(s => s.OpenedAt <= query.ToDate.Value);

            // SORTING
            sessions = query.SortBy?.ToLower() switch
            {
                "openedat" => query.Desc ? sessions.OrderByDescending(s => s.OpenedAt) : sessions.OrderBy(s => s.OpenedAt),
                "closedat" => query.Desc ? sessions.OrderByDescending(s => s.ClosedAt) : sessions.OrderBy(s => s.ClosedAt),
                "difference" => query.Desc ? sessions.OrderByDescending(s => s.Difference) : sessions.OrderBy(s => s.Difference),
                _ => query.Desc ? sessions.OrderByDescending(s => s.OpenedAt) : sessions.OrderBy(s => s.OpenedAt)
            };

            var total = sessions.Count();
            var items = sessions
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<CashSessionResult>
            {
                Items = _mapper.Map<List<CashSessionResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}