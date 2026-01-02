using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CashSessionService
    {
        private readonly IRepository<CashSession> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CashSessionService(
            IRepository<CashSession> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CREATE
        public async Task<CashSessionResult> CreateAsync(CreateCashSessionRequest request)
        {
            var cashSession = _mapper.Map<CashSession>(request);

            cashSession.Id = Guid.NewGuid();
            cashSession.CreatedAt = DateTime.UtcNow;
            cashSession.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(cashSession);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashSessionResult>(cashSession);
        }

        // GET BY ID
        public async Task<CashSessionResult> GetByIdAsync(Guid id)
        {
            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null || cashSession.IsDeleted)
            {
                throw new NotFoundException("CashSession", id);
            }

            return _mapper.Map<CashSessionResult>(cashSession);
        }

        // GET ALL
        public async Task<List<CashSessionResult>> GetAllAsync()
        {
            var cashSessions = await _repository.GetAllAsync();

            // Filter out soft-deleted cashSessions
            var activeCashSessions = cashSessions.Where(p => !p.IsDeleted).ToList();

            return _mapper.Map<List<CashSessionResult>>(activeCashSessions);
        }

        // UPDATE
        public async Task<CashSessionResult> UpdateAsync(Guid id, UpdateCashSessionRequest request)
        {
            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null || cashSession.IsDeleted)
            {
                throw new NotFoundException("CashSession", id);
            }

            // Map the request to the cashSession
            _mapper.Map(request, cashSession);

            // Always update the ModifiedAt timestamp
            cashSession.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashSession);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashSessionResult>(cashSession);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var cashSession = await _repository.GetByIdAsync(id);

            if (cashSession is null || cashSession.IsDeleted)
            {
                throw new NotFoundException("CashSession", id);
            }

            cashSession.IsDeleted = true;
            cashSession.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashSession);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<CashSessionResult>> QueryAsync(CashSessionQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Paging", new[] { "Invalid paging parameters." } }
        });

            var sessions = (await _repository.GetAllAsync())
                .Where(s => !s.IsDeleted)
                .AsQueryable();

            // =========================
            // FILTERS
            // =========================

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

            // =========================
            // SORTING
            // =========================
            sessions = query.SortBy.ToLower() switch
            {
                "openedat" => query.Desc
                    ? sessions.OrderByDescending(s => s.OpenedAt)
                    : sessions.OrderBy(s => s.OpenedAt),

                "closedat" => query.Desc
                    ? sessions.OrderByDescending(s => s.ClosedAt)
                    : sessions.OrderBy(s => s.ClosedAt),

                "difference" => query.Desc
                    ? sessions.OrderByDescending(s => s.Difference)
                    : sessions.OrderBy(s => s.Difference),

                _ => query.Desc
                    ? sessions.OrderByDescending(s => s.OpenedAt)
                    : sessions.OrderBy(s => s.OpenedAt)
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
