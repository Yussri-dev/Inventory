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

            // Filter out soft-deleted cashMovements
            var filtered = all.Where(p => !p.IsDeleted).AsQueryable();

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "name" => query.Desc
                    ? filtered.OrderByDescending(p => p.CashSales)
                    : filtered.OrderBy(p => p.CashSales),

                "salePrice" => query.Desc
                    ? filtered.OrderByDescending(p => p.GeneratedAt)
                    : filtered.OrderBy(p => p.GeneratedAt),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
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
