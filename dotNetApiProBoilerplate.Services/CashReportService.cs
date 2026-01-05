using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CashReports.Requests;
using Inventory.Dto.CashReports.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CashReportService
    {
        private readonly IRepository<CashReport> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public CashReportService(
            IRepository<CashReport> repository,
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
        public async Task<CashReportResult> CreateAsync(CreateCashReportRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var cashReport = _mapper.Map<CashReport>(request);

            cashReport.Id = Guid.NewGuid();
            cashReport.TenantId = tenantId;
            cashReport.CreatedByUserId = userId;
            cashReport.GeneratedByUserId = userId;
            cashReport.GeneratedAt = DateTime.UtcNow;
            cashReport.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(cashReport);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashReportResult>(cashReport);
        }

        // GET BY ID
        public async Task<CashReportResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var cashReport = await _repository.GetByIdAsync(id);

            if (cashReport is null || cashReport.IsDeleted || cashReport.TenantId != tenantId)
            {
                throw new NotFoundException("CashReport", id);
            }

            return _mapper.Map<CashReportResult>(cashReport);
        }

        // GET ALL
        public async Task<List<CashReportResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var cashReports = await _repository.GetAllAsync();

            var activeCashReports = cashReports
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<CashReportResult>>(activeCashReports);
        }

        // UPDATE
        public async Task<CashReportResult> UpdateAsync(Guid id, UpdateCashReportRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var cashReport = await _repository.GetByIdAsync(id);

            if (cashReport is null || cashReport.IsDeleted || cashReport.TenantId != tenantId)
            {
                throw new NotFoundException("CashReport", id);
            }

            _mapper.Map(request, cashReport);
            cashReport.ModifiedAt = DateTime.UtcNow;
            cashReport.ModifiedByUserId = userId;

            _repository.Update(cashReport);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashReportResult>(cashReport);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var cashReport = await _repository.GetByIdAsync(id);

            if (cashReport is null || cashReport.IsDeleted || cashReport.TenantId != tenantId)
            {
                throw new NotFoundException("CashReport", id);
            }

            cashReport.IsDeleted = true;
            cashReport.DeletedAt = DateTime.UtcNow;
            cashReport.DeletedByUserId = userId;
            cashReport.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashReport);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<CashReportResult>> QueryAsync(CashReportQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });

            var reports = (await _repository.GetAllAsync())
                .Where(r => !r.IsDeleted && r.TenantId == tenantId)
                .AsQueryable();

            // FILTERS
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

            // SORTING
            reports = query.SortBy.ToLower() switch
            {
                "generatedat" => query.Desc ? reports.OrderByDescending(r => r.GeneratedAt) : reports.OrderBy(r => r.GeneratedAt),
                "difference" => query.Desc ? reports.OrderByDescending(r => r.Difference) : reports.OrderBy(r => r.Difference),
                "expectedamount" => query.Desc ? reports.OrderByDescending(r => r.ExpectedAmount) : reports.OrderBy(r => r.ExpectedAmount),
                _ => query.Desc ? reports.OrderByDescending(r => r.GeneratedAt) : reports.OrderBy(r => r.GeneratedAt)
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