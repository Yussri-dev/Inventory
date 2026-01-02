using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CashReports.Requests;
using Inventory.Dto.CashReports.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CashReportService
    {
        private readonly IRepository<CashReport> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CashReportService(
            IRepository<CashReport> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CREATE
        public async Task<CashReportResult> CreateAsync(CreateCashReportRequest request)
        {
            var cashMovement = _mapper.Map<CashReport>(request);

            cashMovement.Id = Guid.NewGuid();
            cashMovement.CreatedAt = DateTime.UtcNow;
            cashMovement.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(cashMovement);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashReportResult>(cashMovement);
        }

        // GET BY ID
        public async Task<CashReportResult> GetByIdAsync(Guid id)
        {
            var cashMovement = await _repository.GetByIdAsync(id);

            if (cashMovement is null || cashMovement.IsDeleted)
            {
                throw new NotFoundException("CashReport", id);
            }

            return _mapper.Map<CashReportResult>(cashMovement);
        }

        // GET ALL
        public async Task<List<CashReportResult>> GetAllAsync()
        {
            var cashMovements = await _repository.GetAllAsync();

            // Filter out soft-deleted cashMovements
            var activeCashReports = cashMovements.Where(p => !p.IsDeleted).ToList();

            return _mapper.Map<List<CashReportResult>>(activeCashReports);
        }

        // UPDATE
        public async Task<CashReportResult> UpdateAsync(Guid id, UpdateCashReportRequest request)
        {
            var cashMovement = await _repository.GetByIdAsync(id);

            if (cashMovement is null || cashMovement.IsDeleted)
            {
                throw new NotFoundException("CashReport", id);
            }

            // Map the request to the cashMovement
            _mapper.Map(request, cashMovement);

            // Always update the ModifiedAt timestamp
            cashMovement.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashMovement);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashReportResult>(cashMovement);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var cashMovement = await _repository.GetByIdAsync(id);

            if (cashMovement is null || cashMovement.IsDeleted)
            {
                throw new NotFoundException("CashReport", id);
            }

            cashMovement.IsDeleted = true;
            cashMovement.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashMovement);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<CashReportResult>> QueryAsync(CashReportQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Paging", new[] { "Invalid paging parameters." } }
        });

            var reports = (await _repository.GetAllAsync())
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            // =========================
            // FILTERS
            // =========================
            if (query.CashSessionId.HasValue)
                reports = reports.Where(r => r.CashSessionId == query.CashSessionId.Value);

            if (!string.IsNullOrWhiteSpace(query.Type))
                reports = reports.Where(r => r.Type == query.Type);

            if (query.GeneratedByUserId.HasValue)
                reports = reports.Where(r => r.GeneratedByUserId == query.GeneratedByUserId.Value);

            if (query.FromDate.HasValue)
                reports = reports.Where(r => r.GeneratedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                reports = reports.Where(r => r.GeneratedAt <= query.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
                reports = reports.Where(r =>
                    (r.Notes != null && r.Notes.Contains(query.Search)) ||
                    r.Type.Contains(query.Search)
                );

            // =========================
            // SORTING
            // =========================
            reports = query.SortBy.ToLower() switch
            {
                "generatedat" => query.Desc
                    ? reports.OrderByDescending(r => r.GeneratedAt)
                    : reports.OrderBy(r => r.GeneratedAt),

                "difference" => query.Desc
                    ? reports.OrderByDescending(r => r.Difference)
                    : reports.OrderBy(r => r.Difference),

                "expectedamount" => query.Desc
                    ? reports.OrderByDescending(r => r.ExpectedAmount)
                    : reports.OrderBy(r => r.ExpectedAmount),

                _ => query.Desc
                    ? reports.OrderByDescending(r => r.GeneratedAt)
                    : reports.OrderBy(r => r.GeneratedAt)
            };

            var total = reports.Count();

            var items = reports
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<CashReportResult>
            {
                Items = _mapper.Map<List<CashReportResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

    }
}
