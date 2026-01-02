using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.CashMovements.Requests;
using Inventory.Dto.CashMovements.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CashMovementService
    {
        private readonly IRepository<CashMovement> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CashMovementService(
            IRepository<CashMovement> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CREATE
        public async Task<CashMovementResult> CreateAsync(CreateCashMovementRequest request)
        {
            var cashMovement = _mapper.Map<CashMovement>(request);

            cashMovement.Id = Guid.NewGuid();
            cashMovement.CreatedAt = DateTime.UtcNow;
            cashMovement.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(cashMovement);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashMovementResult>(cashMovement);
        }

        // GET BY ID
        public async Task<CashMovementResult> GetByIdAsync(Guid id)
        {
            var cashMovement = await _repository.GetByIdAsync(id);

            if (cashMovement is null || cashMovement.IsDeleted)
            {
                throw new NotFoundException("CashMovement", id);
            }

            return _mapper.Map<CashMovementResult>(cashMovement);
        }

        // GET ALL
        public async Task<List<CashMovementResult>> GetAllAsync()
        {
            var cashMovements = await _repository.GetAllAsync();

            // Filter out soft-deleted cashMovements
            var activeCashMovements = cashMovements.Where(p => !p.IsDeleted).ToList();

            return _mapper.Map<List<CashMovementResult>>(activeCashMovements);
        }

        // UPDATE
        public async Task<CashMovementResult> UpdateAsync(Guid id, UpdateCashMovementRequest request)
        {
            var cashMovement = await _repository.GetByIdAsync(id);

            if (cashMovement is null || cashMovement.IsDeleted)
            {
                throw new NotFoundException("CashMovement", id);
            }

            // Map the request to the cashMovement
            _mapper.Map(request, cashMovement);

            // Always update the ModifiedAt timestamp
            cashMovement.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashMovement);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashMovementResult>(cashMovement);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var cashMovement = await _repository.GetByIdAsync(id);

            if (cashMovement is null || cashMovement.IsDeleted)
            {
                throw new NotFoundException("CashMovement", id);
            }

            cashMovement.IsDeleted = true;
            cashMovement.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashMovement);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<CashMovementResult>> QueryAsync(CashMovementQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Paging", new[] { "Invalid paging parameters." } }
        });

            var movements = (await _repository.GetAllAsync())
                .Where(m => !m.IsDeleted)
                .AsQueryable();

            // =========================
            // FILTERS
            // =========================
            if (query.CashSessionId.HasValue)
                movements = movements.Where(m => m.CashSessionId == query.CashSessionId.Value);

            if (query.SaleId.HasValue)
                movements = movements.Where(m => m.SaleId == query.SaleId.Value);

            if (query.Type.HasValue)
                movements = movements.Where(m => m.Type == (CashMovementType)query.Type.Value);

            if (query.FromDate.HasValue)
                movements = movements.Where(m => m.MovementDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                movements = movements.Where(m => m.MovementDate <= query.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
                movements = movements.Where(m =>
                    (m.Reason != null && m.Reason.Contains(query.Search)) ||
                    m.Amount.ToString().Contains(query.Search)
                );

            // =========================
            // SORTING
            // =========================
            movements = query.SortBy.ToLower() switch
            {
                "amount" => query.Desc
                    ? movements.OrderByDescending(m => m.Amount)
                    : movements.OrderBy(m => m.Amount),

                "type" => query.Desc
                    ? movements.OrderByDescending(m => m.Type)
                    : movements.OrderBy(m => m.Type),

                "movementdate" => query.Desc
                    ? movements.OrderByDescending(m => m.MovementDate)
                    : movements.OrderBy(m => m.MovementDate),

                _ => query.Desc
                    ? movements.OrderByDescending(m => m.MovementDate)
                    : movements.OrderBy(m => m.MovementDate)
            };

            var total = movements.Count();

            var items = movements
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<CashMovementResult>
            {
                Items = _mapper.Map<List<CashMovementResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

    }
}
