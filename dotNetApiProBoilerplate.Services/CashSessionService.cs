using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CashSessionService
    {
        private readonly IRepository<CashSession> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public CashSessionService(
            IRepository<CashSession> repository,
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
        public async Task<CashSessionResult> CreateAsync(CreateCashSessionRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var cashSession = _mapper.Map<CashSession>(request);

            cashSession.Id = Guid.NewGuid();
            cashSession.TenantId = tenantId;
            cashSession.CreatedByUserId = userId;
            cashSession.OpenedByUserId = userId;
            cashSession.OpenedAt = DateTime.UtcNow;
            cashSession.Status = CashSessionStatus.Open;
            cashSession.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(cashSession);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashSessionResult>(cashSession);
        }

        // GET BY ID
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

        // GET ALL
        public async Task<List<CashSessionResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var cashSessions = await _repository.GetAllAsync();

            var activeCashSessions = cashSessions
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<CashSessionResult>>(activeCashSessions);
        }

        // UPDATE
        public async Task<CashSessionResult> UpdateAsync(Guid id, UpdateCashSessionRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null || cashSession.IsDeleted || cashSession.TenantId != tenantId)
            {
                throw new NotFoundException("CashSession", id);
            }

            _mapper.Map(request, cashSession);
            cashSession.ModifiedAt = DateTime.UtcNow;
            cashSession.ModifiedByUserId = userId;

            _repository.Update(cashSession);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashSessionResult>(cashSession);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null || cashSession.IsDeleted || cashSession.TenantId != tenantId)
            {
                throw new NotFoundException("CashSession", id);
            }

            cashSession.IsDeleted = true;
            cashSession.DeletedAt = DateTime.UtcNow;
            cashSession.DeletedByUserId = userId;
            cashSession.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashSession);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
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
                    s.SessionNumber.Contains(query.Search) ||
                    (s.OpeningNotes != null && s.OpeningNotes.Contains(query.Search)) ||
                    (s.ClosingNotes != null && s.ClosingNotes.Contains(query.Search))
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
            sessions = query.SortBy.ToLower() switch
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