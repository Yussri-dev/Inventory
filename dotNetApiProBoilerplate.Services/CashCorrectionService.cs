using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CashCorrections.Requests;
using Inventory.Dto.CashCorrections.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CashCorrectionService
    {
        private readonly IRepository<CashCorrection> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CashCorrectionService(
            IRepository<CashCorrection> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CREATE
        public async Task<CashCorrectionResult> CreateAsync(CreateCashCorrectionRequest request)
        {
            var cashCorrection = _mapper.Map<CashCorrection>(request);

            cashCorrection.Id = Guid.NewGuid();
            cashCorrection.CreatedAt = DateTime.UtcNow;
            cashCorrection.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(cashCorrection);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashCorrectionResult>(cashCorrection);
        }

        // GET BY ID
        public async Task<CashCorrectionResult> GetByIdAsync(Guid id)
        {
            var cashCorrection = await _repository.GetByIdAsync(id);

            if (cashCorrection is null || cashCorrection.IsDeleted)
            {
                throw new NotFoundException("CashCorrection", id);
            }

            return _mapper.Map<CashCorrectionResult>(cashCorrection);
        }

        // GET ALL
        public async Task<List<CashCorrectionResult>> GetAllAsync()
        {
            var cashCorrections = await _repository.GetAllAsync();

            // Filter out soft-deleted cashCorrections
            var activeCashCorrections = cashCorrections.Where(p => !p.IsDeleted).ToList();

            return _mapper.Map<List<CashCorrectionResult>>(activeCashCorrections);
        }

        // UPDATE
        public async Task<CashCorrectionResult> UpdateAsync(Guid id, UpdateCashCorrectionRequest request)
        {
            var cashCorrection = await _repository.GetByIdAsync(id);

            if (cashCorrection is null || cashCorrection.IsDeleted)
            {
                throw new NotFoundException("CashCorrection", id);
            }

            // Map the request to the cashCorrection
            _mapper.Map(request, cashCorrection);

            // Always update the ModifiedAt timestamp
            cashCorrection.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashCorrection);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashCorrectionResult>(cashCorrection);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var cashCorrection = await _repository.GetByIdAsync(id);

            if (cashCorrection is null || cashCorrection.IsDeleted)
            {
                throw new NotFoundException("CashCorrection", id);
            }

            cashCorrection.IsDeleted = true;
            cashCorrection.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashCorrection);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<CashCorrectionResult>> QueryAsync(CashCorrectionQuery query)
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

            // Filter out soft-deleted cashCorrections
            var filtered = all.Where(p => !p.IsDeleted).AsQueryable();

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "name" => query.Desc
                    ? filtered.OrderByDescending(p => p.Amount)
                    : filtered.OrderBy(p => p.Amount),

                "salePrice" => query.Desc
                    ? filtered.OrderByDescending(p => p.Reason)
                    : filtered.OrderBy(p => p.Reason),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<CashCorrectionResult>
            {
                Items = _mapper.Map<List<CashCorrectionResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
