using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.StockMouvements.Requests;
using Inventory.Dto.StockMouvements.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class StockMouvementService
    {
        private readonly IRepository<StockMovement> _movementRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public StockMouvementService(
            IRepository<StockMovement> movementRepository,
            IRepository<Stock> stockRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _movementRepository = movementRepository;
            _stockRepository = stockRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<StockMouvementResult> CreateAsync(CreateStockMouvementRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();

            var stock = await _stockRepository
                .GetSingleAsync(s =>
                    s.ProductId == request.ProductId &&
                    !s.IsDeleted &&
                    s.TenantId == tenantId);

            //if (stock == null)
            //{
            //    throw new NotFoundException("Stock for product", request.ProductId);
            //}

            if (request.QuantityChange == 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "QuantityChange", new[] { "QuantityChange cannot be zero." } }
                });
            }

            var quantityBefore = stock.Quantity;
            var quantityAfter = quantityBefore + request.QuantityChange;

            if (quantityAfter < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "QuantityChange", new[] { "Resulting stock quantity cannot be negative." } }
                });
            }

            var movement = _mapper.Map<StockMovement>(request);

            movement.Id = Guid.NewGuid();
            movement.TenantId = tenantId;
            movement.QuantityBefore = quantityBefore;
            movement.QuantityAfter = quantityAfter;
            movement.MovementDate = DateTime.UtcNow;
            movement.CreatedAt = DateTime.UtcNow;
            movement.ModifiedAt = DateTime.UtcNow;

            // Apply stock update
            stock.Quantity = quantityAfter;
            stock.LastUpdated = DateTime.UtcNow;
            stock.ModifiedAt = DateTime.UtcNow;

            await _movementRepository.AddAsync(movement);
            _stockRepository.Update(stock);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<StockMouvementResult>(movement);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<StockMouvementResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var movement = await _movementRepository.GetByIdAsync(id);

            if (movement == null || movement.IsDeleted || movement.TenantId != tenantId)
            {
                throw new NotFoundException("StockMovement", id);
            }

            return _mapper.Map<StockMouvementResult>(movement);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<StockMouvementResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var movements = await _movementRepository.GetAllAsync();

            return _mapper.Map<List<StockMouvementResult>>(
                movements.Where(m => !m.IsDeleted && m.TenantId == tenantId).ToList()
            );
        }

        // =========================
        // UPDATE (metadata only)
        // =========================
        public async Task<StockMouvementResult> UpdateAsync(Guid id, UpdateStockMouvementRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var movement = await _movementRepository.GetByIdAsync(id);

            if (movement == null || movement.IsDeleted || movement.TenantId != tenantId)
            {
                throw new NotFoundException("StockMovement", id);
            }

            _mapper.Map(request, movement);

            movement.ModifiedAt = DateTime.UtcNow;

            _movementRepository.Update(movement);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<StockMouvementResult>(movement);
        }

        // =========================
        // SOFT DELETE (audit safe)
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var movement = await _movementRepository.GetByIdAsync(id);

            if (movement == null || movement.IsDeleted || movement.TenantId != tenantId)
            {
                throw new NotFoundException("StockMovement", id);
            }

            movement.IsDeleted = true;
            movement.ModifiedAt = DateTime.UtcNow;

            _movementRepository.Update(movement);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<StockMouvementResult>> QueryAsync(StockMouvementQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var movements = (await _movementRepository.GetAllAsync())
                .Where(m => !m.IsDeleted && m.TenantId == tenantId)
                .AsQueryable();

            // =========================
            // FILTERS
            // =========================
            if (query.ProductId.HasValue)
            {
                movements = movements.Where(m => m.ProductId == query.ProductId.Value);
            }

            if (query.Type.HasValue)
            {
                var domainType = (Domain.Enums.StockMovementType)query.Type.Value;
                movements = movements.Where(m => m.Type == domainType);
            }

            if (query.FromDate.HasValue)
            {
                movements = movements.Where(m => m.MovementDate >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                movements = movements.Where(m => m.MovementDate <= query.ToDate.Value);
            }

            // =========================
            // SORTING
            // =========================
            movements = query.SortBy?.ToLower() switch
            {
                "movementdate" => query.Desc
                    ? movements.OrderByDescending(m => m.MovementDate)
                    : movements.OrderBy(m => m.MovementDate),

                "quantitychange" => query.Desc
                    ? movements.OrderByDescending(m => m.QuantityChange)
                    : movements.OrderBy(m => m.QuantityChange),

                _ => query.Desc
                    ? movements.OrderByDescending(m => m.CreatedAt)
                    : movements.OrderBy(m => m.CreatedAt)
            };

            var total = movements.Count();

            var items = movements
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<StockMouvementResult>
            {
                Items = _mapper.Map<List<StockMouvementResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
