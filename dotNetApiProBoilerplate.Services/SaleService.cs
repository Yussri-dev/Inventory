using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class SaleService
    {
        private readonly IRepository<Sale> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SaleService(
            IRepository<Sale> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CREATE
        // CREATE
        public async Task<SaleResult> CreateAsync(CreateSaleRequest request)
        {
            var exists = await _repository.ExistsAsync(
                p => p.InvoiceNumber == request.InvoiceNumber && !p.IsDeleted);

            if (exists)
            {
                throw new ConflictException(
                    $"Sale with Invoice number '{request.InvoiceNumber}' already exists.");

            }

            if (request.TotalAmount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmount", new[] { "TotalAmount must be > 0." } }
                });

            }

            var product = _mapper.Map<Sale>(request);

            product.Id = Guid.NewGuid();

            product.SaleDate = request.SaleDate == default
                ? DateTime.UtcNow
                : request.SaleDate;

            product.TotalAmount = request.TotalAmount;

            product.CreatedAt = DateTime.UtcNow;
            product.ModifiedAt = DateTime.UtcNow;
            // =========================

            await _repository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(product);
        }

        // GET BY ID
        public async Task<SaleResult> GetByIdAsync(Guid id)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted)
            {
                throw new NotFoundException("Sale", id);
            }

            return _mapper.Map<SaleResult>(product);
        }

        // GET ALL
        public async Task<List<SaleResult>> GetAllAsync()
        {
            var products = await _repository.GetAllAsync();

            // Filter out soft-deleted products
            var activeSales = products.Where(p => !p.IsDeleted).ToList();

            return _mapper.Map<List<SaleResult>>(activeSales);
        }

        // UPDATE
        public async Task<SaleResult> UpdateAsync(Guid id, UpdateSaleRequest request)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted)
            {
                throw new NotFoundException("Sale", id);
            }

            // Uniqueness check
            if (!string.IsNullOrWhiteSpace(request.InvoiceNumber) &&
                request.InvoiceNumber != product.InvoiceNumber)
            {
                var nameExists = await _repository.ExistsAsync(p =>
                    p.InvoiceNumber == request.InvoiceNumber &&
                    p.Id != id &&
                    !p.IsDeleted);

                if (nameExists)
                {
                    throw new ConflictException(
                        $"Sale with number '{request.InvoiceNumber}' already exists.");
                }
            }

            // Validation
            if (request.TotalAmount < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmount", new[] { "TotalAmount must be >= 0." } }
                });
            }

            // Base mapping (respects your AutoMapper profile)
            _mapper.Map(request, product);

            // Totals are ignored by AutoMapper → must be reassigned
            product.TotalAmount = request.TotalAmount;

            // Audit
            product.ModifiedAt = DateTime.UtcNow;

            _repository.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(product);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted)
            {
                throw new NotFoundException("Sale", id);
            }

            product.IsDeleted = true;
            product.ModifiedAt = DateTime.UtcNow;

            _repository.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<SaleResult>> QueryAsync(SaleQuery query)
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

            // Filter out soft-deleted products
            var filtered = all.Where(p => !p.IsDeleted).AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                filtered = filtered.Where(p =>
                    p.InvoiceNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    (p.InvoiceNumber != null && p.InvoiceNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase)));
            }

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "SaleNumber" => query.Desc
                    ? filtered.OrderByDescending(p => p.InvoiceNumber)
                    : filtered.OrderBy(p => p.InvoiceNumber),

                "SaleDate" => query.Desc
                    ? filtered.OrderByDescending(p => p.SaleDate)
                    : filtered.OrderBy(p => p.SaleDate),

                "Name" => query.Desc
                    ? filtered.OrderByDescending(p => p.Customer.Name)
                    : filtered.OrderBy(p => p.Customer.Name),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<SaleResult>
            {
                Items = _mapper.Map<List<SaleResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}