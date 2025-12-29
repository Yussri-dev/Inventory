using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Stock.Requests;
using Inventory.Dto.Stock.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class StockService
    {
        private readonly IRepository<Stock> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StockService(
            IRepository<Stock> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<StockResult> CreateAsync(CreateStockRequest request)
        {
            // One stock per product
            var exists = await _repository.ExistsAsync(
                s => s.ProductId == request.ProductId && !s.IsDeleted);

            if (exists)
            {
                throw new ConflictException(
                    $"Stock already exists for product '{request.ProductId}'.");
            }

            if (request.Quantity < 0 || request.ReservedQuantity < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Quantity", new[] { "Quantities must be >= 0." } }
                });
            }

            if (request.ReservedQuantity > request.Quantity)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "ReservedQuantity", new[] { "Reserved quantity cannot exceed total quantity." } }
                });
            }

            var stock = _mapper.Map<Stock>(request);

            stock.Id = Guid.NewGuid();
            stock.LastUpdated = DateTime.UtcNow;
            stock.CreatedAt = DateTime.UtcNow;
            stock.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(stock);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<StockResult>(stock);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<StockResult> GetByIdAsync(Guid id)
        {
            var stock = await _repository.GetByIdAsync(id);

            if (stock is null || stock.IsDeleted)
            {
                throw new NotFoundException("Stock", id);
            }

            return _mapper.Map<StockResult>(stock);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<StockResult>> GetAllAsync()
        {
            var stocks = await _repository.GetAllAsync();

            return _mapper.Map<List<StockResult>>(
                stocks.Where(s => !s.IsDeleted).ToList());
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<StockResult> UpdateAsync(Guid id, UpdateStockRequest request)
        {
            var stock = await _repository.GetByIdAsync(id);

            if (stock is null || stock.IsDeleted)
            {
                throw new NotFoundException("Stock", id);
            }

            if (request.Quantity < 0 || request.ReservedQuantity < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Quantity", new[] { "Quantities must be >= 0." } }
                });
            }

            if (request.ReservedQuantity > request.Quantity)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "ReservedQuantity", new[] { "Reserved quantity cannot exceed total quantity." } }
                });
            }

            _mapper.Map(request, stock);

            stock.LastUpdated = DateTime.UtcNow;
            stock.ModifiedAt = DateTime.UtcNow;

            _repository.Update(stock);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<StockResult>(stock);
        }

        // =========================
        // SOFT DELETE
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var stock = await _repository.GetByIdAsync(id);

            if (stock is null || stock.IsDeleted)
            {
                throw new NotFoundException("Stock", id);
            }

            stock.IsDeleted = true;
            stock.ModifiedAt = DateTime.UtcNow;

            _repository.Update(stock);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<StockResult>> QueryAsync(StockQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var all = (await _repository.GetAllAsync())
                .Where(s => !s.IsDeleted)
                .AsQueryable();

            if (query.ProductId.HasValue)
            {
                all = all.Where(s => s.ProductId == query.ProductId.Value);
            }

            var total = all.Count();

            var items = all
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<StockResult>
            {
                Items = _mapper.Map<List<StockResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
