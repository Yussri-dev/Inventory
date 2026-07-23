using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Enums;
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
        private readonly IRepository<CashMovement> _cashMovementRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public CashSessionService(
            IRepository<CashSession> repository,
            IRepository<CashMovement> cashMovementRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
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
        public async Task<CashSessionResult> CreateAsync(
            CreateCashSessionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            if (request.ClientOperationId ==
                Guid.Empty)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(request.ClientOperationId),
                            new[]
                            {
                                "ClientOperationId is required."
                            }
                        }
                    });
            }

            if (request.OpeningAmount <
                0m)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(request.OpeningAmount),
                            new[]
                            {
                                "OpeningAmount cannot be negative."
                            }
                        }
                    });
            }

            /*
             * Check idempotency first. A retry of the same local
             * operation returns the previously created server session.
             */
            var existingByOperation =
                await _repository.GetSingleAsync(
                    session =>
                        session.TenantId == tenantId &&
                        session.ClientOperationId ==
                            request.ClientOperationId &&
                        !session.IsDeleted);

            if (existingByOperation != null)
            {
                return _mapper.Map<CashSessionResult>(
                    existingByOperation);
            }

            /*
             * An open session with another ClientOperationId is a real
             * business conflict and must not be linked automatically.
             */
            var existingOpenSession =
                await _repository.GetSingleAsync(
                    session =>
                        session.TenantId == tenantId &&
                        session.Status ==
                            CashSessionStatus.Open &&
                        !session.IsDeleted);

            if (existingOpenSession != null)
            {
                throw new ConflictException(
                    $"A different cash session is already open " +
                    $"(Session #{existingOpenSession.SessionNumber}). " +
                    "Close it before uploading this offline session.");
            }

            var now =
                DateTime.UtcNow;

            var openedAt =
                EnsureUtc(
                    request.OpenedAtUtc ??
                    now);

            var openingAmount =
                RoundMoney(
                    request.OpeningAmount);

            var cashSession =
                _mapper.Map<CashSession>(
                    request);

            cashSession.Id =
                Guid.NewGuid();

            cashSession.TenantId =
                tenantId;

            cashSession.ClientOperationId =
                request.ClientOperationId;

            cashSession.CreatedByUserId =
                userId;

            cashSession.OpenedByUserId =
                userId;

            cashSession.OpenedAt =
                openedAt;

            cashSession.Status =
                CashSessionStatus.Open;

            cashSession.OpeningAmount =
                openingAmount;

            cashSession.ClosingAmountExpected =
                openingAmount;

            cashSession.ClosingAmountCounted =
                0m;

            cashSession.Difference =
                0m;

            cashSession.OpeningNotes =
                NormalizeNullable(
                    request.OpeningNotes);

            cashSession.CreatedAt =
                now;

            cashSession.ModifiedAt =
                now;

            cashSession.SessionNumber =
                GenerateSessionNumber(
                    openedAt);

            await _repository.AddAsync(
                cashSession);

            await _cashMovementRepository.AddAsync(
                new CashMovement
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    CashSessionId =
                        cashSession.Id,

                    Type =
                        CashMovementType.Opening,

                    Amount =
                        openingAmount,

                    BalanceBefore =
                        0m,

                    BalanceAfter =
                        openingAmount,

                    Reason =
                        "Session opening",

                    MovementDate =
                        openedAt,

                    CreatedAt =
                        now
                });

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashSessionResult>(
                cashSession);
        }


        // =========================
        // CLOSE SESSION
        // =========================
        public async Task<CashSessionResult> CloseSessionAsync(
            Guid id,
            CloseCashSessionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (id == Guid.Empty)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(id),
                            new[]
                            {
                                "Cash session id is required."
                            }
                        }
                    });
            }

            if (request.ActualCash < 0m)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(request.ActualCash),
                            new[]
                            {
                                "ActualCash cannot be negative."
                            }
                        }
                    });
            }

            var userId =
                _tenantContext.UserId;

            var cashSession =
                await _repository.GetByIdAsync(id);

            if (cashSession == null ||
                cashSession.IsDeleted ||
                !CanAccessTenant(cashSession.TenantId))
            {
                throw new NotFoundException(
                    "CashSession",
                    id);
            }

            /*
             * Idempotent close:
             * when the server committed but the response was lost,
             * the retry returns the already-closed session.
             */
            if (cashSession.Status ==
                CashSessionStatus.Closed)
            {
                return _mapper.Map<CashSessionResult>(
                    cashSession);
            }

            if (cashSession.Status !=
                CashSessionStatus.Open)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(cashSession.Status),
                            new[]
                            {
                                "Cash session is not open."
                            }
                        }
                    });
            }

            var cashMovements =
                (await _cashMovementRepository.GetAllAsync())
                    .Where(movement =>
                        movement.CashSessionId == id &&
                        movement.TenantId ==
                            cashSession.TenantId &&
                        !movement.IsDeleted)
                    .OrderBy(movement =>
                        movement.MovementDate)
                    .ThenBy(movement =>
                        movement.CreatedAt)
                    .ToList();

            /*
             * BalanceAfter is authoritative. OpeningAmount is the
             * fallback for legacy sessions without movement rows.
             */
            var expectedCash =
                RoundMoney(
                    cashMovements.LastOrDefault()?.BalanceAfter
                    ?? cashSession.OpeningAmount);

            var actualCash =
                RoundMoney(
                    request.ActualCash);

            var now =
                DateTime.UtcNow;

            cashSession.ClosingAmountExpected =
                expectedCash;

            cashSession.ClosingAmountCounted =
                actualCash;

            cashSession.Difference =
                RoundMoney(
                    actualCash -
                    expectedCash);

            cashSession.ClosingNotes =
                NormalizeNullable(
                    request.ClosingNotes);

            cashSession.Status =
                CashSessionStatus.Closed;

            cashSession.ClosedAt =
                now;

            cashSession.ClosedByUserId =
                userId;

            cashSession.ModifiedAt =
                now;

            cashSession.ModifiedByUserId =
                userId;

            _repository.Update(
                cashSession);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashSessionResult>(
                cashSession);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<CashSessionResult> GetByIdAsync(
            Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new NotFoundException(
                    "CashSession",
                    id);
            }

            var cashSession =
                await _repository.GetByIdAsync(id);

            if (cashSession is null ||
                cashSession.IsDeleted ||
                !CanAccessTenant(cashSession.TenantId))
            {
                throw new NotFoundException(
                    "CashSession",
                    id);
            }

            return _mapper.Map<CashSessionResult>(
                cashSession);
        }


        private IQueryable<CashSession> ApplyTenantScope(IEnumerable<CashSession> source)
        {
            if (_tenantContext.IsSuperAdmin)
                return source.AsQueryable();

            var tenantId = _tenantContext.TenantId;
            return source.Where(s => s.TenantId == tenantId).AsQueryable();
        }

        /*
         * Compatibility wrapper. Keep one canonical implementation and
         * remove this method later if the interface no longer exposes it.
         */
        public Task<CashSessionResult?> GetActiveAsync()
        {
            return GetActiveSessionAsync();
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
            var userId = _tenantContext.UserId;

            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null ||
                cashSession.IsDeleted ||
                !CanAccessTenant(cashSession.TenantId))
            {
                throw new NotFoundException(
                    "CashSession",
                    id);
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
            var userId = _tenantContext.UserId;

            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null ||
                cashSession.IsDeleted ||
                !CanAccessTenant(cashSession.TenantId))
            {
                throw new NotFoundException(
                    "CashSession",
                    id);
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

        private bool CanAccessTenant(
            Guid entityTenantId)
        {
            return _tenantContext.IsSuperAdmin ||
                   entityTenantId ==
                       _tenantContext.TenantId;
        }

        private static string GenerateSessionNumber(
            DateTime openedAtUtc)
        {
            /*
             * Count + 1 is unsafe across concurrent devices and can be
             * reused after deletions. The timestamp plus a random suffix
             * remains readable and practically unique.
             */
            return $"CS-{openedAtUtc:yyyyMMdd-HHmmss}-" +
                   $"{Guid.NewGuid():N}"[..4].ToUpperInvariant();
        }

        private static DateTime EnsureUtc(
            DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc =>
                    value,

                DateTimeKind.Local =>
                    value.ToUniversalTime(),

                _ =>
                    DateTime.SpecifyKind(
                        value,
                        DateTimeKind.Utc)
            };
        }

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static string? NormalizeNullable(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
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