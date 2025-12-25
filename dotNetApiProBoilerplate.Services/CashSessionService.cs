using AutoMapper;
using Inventory.Domain.Entities;
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
            // Validate query parameters
            if (query.Page < 1)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Page", new[] { "Page must be greater than or equal to 1." } }
                };
                throw new ValidationException(errors);
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                };
                throw new ValidationException(errors);
            }

            var all = await _repository.GetAllAsync();

            // Filter out soft-deleted cashSessions
            var filtered = all.Where(p => !p.IsDeleted).AsQueryable();

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "name" => query.Desc
                    ? filtered.OrderByDescending(p => p.CashReports)
                    : filtered.OrderBy(p => p.CashReports),

                "salePrice" => query.Desc
                    ? filtered.OrderByDescending(p => p.OpenedAt)
                    : filtered.OrderBy(p => p.OpenedAt),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
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
