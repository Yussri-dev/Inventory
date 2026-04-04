using AutoMapper;
using AutoMapper.QueryableExtensions;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Stock.Requests;
using Inventory.Dto.Stock.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class StockService
    {
        private readonly IRepository<Stock> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;
        private readonly IReadRepository<Stock> _readRepo;

        public StockService(
            IRepository<Stock> repository,
                IReadRepository<Stock> readRepo,

            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _readRepo = readRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<StockResult> CreateAsync(CreateStockRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            // One stock per product
            var exists = await _repository.ExistsAsync(
                s => s.ProductId == request.ProductId &&
                     s.TenantId == tenantId &&
                     !s.IsDeleted);

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
            stock.TenantId = tenantId;
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
            var tenantId = _tenantContext.TenantId;
            var stock = await _repository.GetByIdAsync(id);

            if (stock is null || stock.IsDeleted || stock.TenantId != tenantId)
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
            var tenantId = _tenantContext.TenantId;

            var stocks = await _repository.GetAsync(
                s => !s.IsDeleted && s.TenantId == tenantId,
                s => s.Product,
                s => s.Product.CatalogProduct
            );

            return _mapper.Map<List<StockResult>>(stocks);
        }


        // =========================
        // UPDATE
        // =========================
        public async Task<StockResult> UpdateAsync(Guid id, UpdateStockRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var stock = await _repository.GetByIdAsync(id);

            if (stock is null || stock.IsDeleted || stock.TenantId != tenantId)
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
            var tenantId = _tenantContext.TenantId;
            var stock = await _repository.GetByIdAsync(id);

            if (stock is null || stock.IsDeleted || stock.TenantId != tenantId)
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
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    {"Page", new[]{"Page must be greater than or equal to 1."} }
                });
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    {"PageSize",new[]{"PageSize must be between 1 and 100."} }
                });
            }


            var queryable = _readRepo
                            .Query()
                            .Include(s => s.Product)
                            .ThenInclude(p => p.CatalogProduct)
                            .Where(s => !s.IsDeleted && s.TenantId == tenantId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();

                queryable = queryable.Where(s =>
                EF.Functions.ILike(s.Product.Name, $"%{search}%") ||
                EF.Functions.ILike(s.Product.CatalogProduct.Name, $"%{search}%")
                );
            }

            queryable = query.SortBy?.ToLower() switch
            {
                "name" => query.Desc
                    ? queryable.OrderByDescending(s => s.Product.Name)
                    : queryable.OrderBy(s => s.Product.Name),

                "barcode" => query.Desc
                    ? queryable.OrderByDescending(s => s.Product.Barcode)
                    : queryable.OrderBy(s => s.Product.Barcode),

                _ => queryable.OrderBy(s => s.Product.Name)
            };

            var total = await queryable.CountAsync();

            var items = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ProjectTo<StockResult>(_mapper.ConfigurationProvider)
                .ToListAsync();


            return new PagedResult<StockResult>
            {
                Items = items,
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
