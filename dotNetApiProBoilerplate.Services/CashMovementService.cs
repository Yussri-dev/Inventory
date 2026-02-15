using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CashMovements.Requests;
using Inventory.Dto.CashMovements.Results;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CashMovementService
    {
        private readonly IRepository<CashMovement> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public CashMovementService(
            IRepository<CashMovement> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        public async Task<CashMovementResult> CreateAsync(CreateCashMovementRequest request)
        {
            if (!_tenantContext.IsAdmin)
                throw new ForbiddenException("Only admins can create cash movements.");

            if (request.Type is CashMovementType.Opening or CashMovementType.Closing)
                throw new ForbiddenException("Opening and closing cash movements are system-controlled.");

            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var lastMovement = await _repository.GetLastAsync(
                m => m.CashSessionId == request.CashSessionId
                     && !m.IsDeleted
                     && m.TenantId == tenantId,
                m => m.MovementDate
            );

            var balanceBefore = lastMovement?.BalanceAfter ?? 0m;

            // ➕ / ➖ Sens du mouvement
            var signedAmount = request.Type.IsOut()
                ? -request.Amount
                : request.Amount;

            var balanceAfter = balanceBefore + signedAmount;

            if (balanceAfter < 0)
                throw new ValidationException("Cash balance cannot be negative.");

            var cashMovement = new CashMovement
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CashSessionId = request.CashSessionId,
                Type = request.Type,
                Amount = request.Amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                SaleId = request.SaleId,
                Reason = request.Reason,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(cashMovement);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashMovementResult>(cashMovement);
        }


        // GET BY ID
        public async Task<CashMovementResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var cashMovement = await _repository.GetByIdAsync(id);

            if (cashMovement is null || cashMovement.IsDeleted || cashMovement.TenantId != tenantId)
            {
                throw new NotFoundException("CashMovement", id);
            }

            return _mapper.Map<CashMovementResult>(cashMovement);
        }

        // GET ALL
        public async Task<List<CashMovementResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;
            var cashMovements = await _repository.GetAllAsync();

            var activeCashMovements = cashMovements
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<CashMovementResult>>(activeCashMovements);
        }

        /*
        // UPDATE
        public async Task<CashMovementResult> UpdateAsync(Guid id, UpdateCashMovementRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var cashMovement = await _repository.GetByIdAsync(id);

            if (cashMovement is null || cashMovement.IsDeleted || cashMovement.TenantId != tenantId)
            {
                throw new NotFoundException("CashMovement", id);
            }

            _mapper.Map(request, cashMovement);
            cashMovement.ModifiedAt = DateTime.UtcNow;
            cashMovement.ModifiedByUserId = userId;

            _repository.Update(cashMovement);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashMovementResult>(cashMovement);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var cashMovement = await _repository.GetByIdAsync(id);

            if (cashMovement is null || cashMovement.IsDeleted || cashMovement.TenantId != tenantId)
            {
                throw new NotFoundException("CashMovement", id);
            }

            cashMovement.IsDeleted = true;
            cashMovement.DeletedAt = DateTime.UtcNow;
            cashMovement.DeletedByUserId = userId;
            cashMovement.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashMovement);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        */
        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<CashMovementResult>> QueryAsync(CashMovementQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });

            var movements = (await _repository.GetAllAsync())
                .Where(m => !m.IsDeleted && m.TenantId == tenantId)
                .AsQueryable();

            // FILTERS
            if (query.CashSessionId.HasValue)
                movements = movements.Where(m => m.CashSessionId == query.CashSessionId.Value);

            if (query.SaleId.HasValue)
                movements = movements.Where(m => m.SaleId == query.SaleId.Value);

            if (query.Type.HasValue)
            {
                var type = query.Type.Value;
                movements = movements.Where(m => m.Type == type);
            }

            if (query.FromDate.HasValue)
                movements = movements.Where(m => m.MovementDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                movements = movements.Where(m => m.MovementDate <= query.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
                movements = movements.Where(m =>
                    (m.Reason != null && m.Reason.Contains(query.Search)) ||
                    m.Amount.ToString().Contains(query.Search)
                );

            // SORTING
            movements = query.SortBy.ToLower() switch
            {
                "amount" => query.Desc ? movements.OrderByDescending(m => m.Amount) : movements.OrderBy(m => m.Amount),
                "type" => query.Desc ? movements.OrderByDescending(m => m.Type) : movements.OrderBy(m => m.Type),
                "movementdate" => query.Desc ? movements.OrderByDescending(m => m.MovementDate) : movements.OrderBy(m => m.MovementDate),
                _ => query.Desc ? movements.OrderByDescending(m => m.MovementDate) : movements.OrderBy(m => m.MovementDate)
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