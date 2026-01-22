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

        private async Task<CashSession?> GetActiveSessionEntityAsync()
        {
            var tenantId = _tenantContext.TenantId;

            return await _repository.GetSingleAsync(
                cs => cs.TenantId == tenantId &&
                      cs.Status == CashSessionStatus.Open &&
                      !cs.IsDeleted);
        }


        // =========================
        // GET ACTIVE SESSION
        // =========================
        public async Task<CashSessionResult?> GetActiveSessionAsync()
        {
            var entity = await GetActiveSessionEntityAsync();
            return entity == null ? null : _mapper.Map<CashSessionResult>(entity);
        }

        // =========================
        // ENSURE ACTIVE SESSION EXISTS
        // =========================
        public async Task<Guid> EnsureActiveSessionAsync()
        {
            var entity = await GetActiveSessionEntityAsync();

            if (entity == null)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "CashSession", new[] { "No active cash session. Please open a cash session before making sales." } }
                });
            }

            return entity.Id;
        }


        // =========================
        // CREATE (OPEN SESSION)
        // =========================
        public async Task<CashSessionResult> CreateAsync(CreateCashSessionRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

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
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession == null || cashSession.IsDeleted)
                throw new NotFoundException("CashSession", id);

            if (!_tenantContext.IsSuperAdmin && cashSession.TenantId != tenantId)
                throw new NotFoundException("CashSession", id);

            if (cashSession.Status != CashSessionStatus.Open)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Status", new[] { "Cash session is not open." } }
                });

            var cashMovements = (await _cashMovementRepository.GetAllAsync())
                .Where(cm =>
                    cm.CashSessionId == id &&
                    !cm.IsDeleted &&
                    (_tenantContext.IsSuperAdmin || cm.TenantId == tenantId))
                .OrderByDescending(cm => cm.MovementDate)
                .ToList();

            var expectedCash =
                cashMovements.FirstOrDefault()?.BalanceAfter
                ?? cashSession.OpeningAmount;

            cashSession.ClosingAmountExpected = expectedCash;
            cashSession.ClosingAmountCounted = request.ActualCash;
            cashSession.Difference = request.ActualCash - expectedCash;
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
            var tenantId = _tenantContext.TenantId;
            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null || cashSession.IsDeleted || cashSession.TenantId != tenantId)
            {
                throw new NotFoundException("CashSession", id);
            }

            return _mapper.Map<CashSessionResult>(cashSession);
        }


        private IQueryable<CashSession> ApplyTenantScope(IEnumerable<CashSession> source)
        {
            if (_tenantContext.IsSuperAdmin)
                return source.AsQueryable();

            var tenantId = _tenantContext.TenantId;
            return source.Where(s => s.TenantId == tenantId).AsQueryable();
        }

        public async Task<CashSessionResult?> GetActiveAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var session = await _repository.GetSingleAsync(
                s => s.TenantId == tenantId
                  && s.Status == CashSessionStatus.Open
                  && !s.IsDeleted
            );

            return session == null
                ? null
                : _mapper.Map<CashSessionResult>(session);
        }



        // =========================
        // GET ALL
        // =========================
        public async Task<List<CashSessionResult>> GetAllAsync()
        {
            var sessions = await _repository.GetAllAsync();

            var scoped = ApplyTenantScope(sessions)
                .Where(s => !s.IsDeleted)
                .ToList();

            return _mapper.Map<List<CashSessionResult>>(scoped);
        }


        // =========================
        // UPDATE
        // =========================
        public async Task<CashSessionResult> UpdateAsync(Guid id, UpdateCashSessionRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

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
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

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
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });

            var sessions = ApplyTenantScope(await _repository.GetAllAsync())
                .Where(s => !s.IsDeleted);


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