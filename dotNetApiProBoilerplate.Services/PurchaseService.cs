using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class PurchaseService
    {
        private readonly IRepository<Purchase> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PurchaseService(
            IRepository<Purchase> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CREATE
        // CREATE
        public async Task<PurchaseResult> CreateAsync(CreatePurchaseRequest request)
        {
            var exists = await _repository.ExistsAsync(
                p => p.PurchaseNumber == request.PurchaseNumber && !p.IsDeleted);

            if (exists)
            {
                throw new ConflictException(
                    $"Purchase with purchase number '{request.PurchaseNumber}' already exists.");

            }

            if (request.TotalAmountInclVat <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmountInclVat", new[] { "TotalAmountInclVat must be > 0." } }
                });

            }

            if (request.TotalAmountExclVat <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmountExclVat", new[] { "TotalAmountExclVat must be > 0." } }
                });
            }

            var product = _mapper.Map<Purchase>(request);

            product.Id = Guid.NewGuid();

            product.PurchaseDate = request.PurchaseDate == default
                ? DateTime.UtcNow
                : request.PurchaseDate;

            product.TotalAmountExclVat = request.TotalAmountExclVat;
            product.TotalVatAmount = request.TotalVatAmount;
            product.TotalAmountInclVat = request.TotalAmountInclVat;

            product.Status = PurchaseStatus.Received;

            product.CreatedAt = DateTime.UtcNow;
            product.ModifiedAt = DateTime.UtcNow;
            // =========================

            await _repository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResult>(product);
        }

        // GET BY ID
        public async Task<PurchaseResult> GetByIdAsync(Guid id)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted)
            {
                throw new NotFoundException("Purchase", id);
            }

            return _mapper.Map<PurchaseResult>(product);
        }

        // GET ALL
        public async Task<List<PurchaseResult>> GetAllAsync()
        {
            var products = await _repository.GetAllAsync();

            // Filter out soft-deleted products
            var activePurchases = products.Where(p => !p.IsDeleted).ToList();

            return _mapper.Map<List<PurchaseResult>>(activePurchases);
        }

        // UPDATE
        public async Task<PurchaseResult> UpdateAsync(Guid id, UpdatePurchaseRequest request)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted)
            {
                throw new NotFoundException("Purchase", id);
            }

            // Uniqueness check
            if (!string.IsNullOrWhiteSpace(request.PurchaseNumber) &&
                request.PurchaseNumber != product.PurchaseNumber)
            {
                var nameExists = await _repository.ExistsAsync(p =>
                    p.PurchaseNumber == request.PurchaseNumber &&
                    p.Id != id &&
                    !p.IsDeleted);

                if (nameExists)
                {
                    throw new ConflictException(
                        $"Purchase with number '{request.PurchaseNumber}' already exists.");
                }
            }

            // Validation
            if (request.TotalAmountInclVat < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmountInclVat", new[] { "TotalAmountInclVat must be >= 0." } }
                });
            }

            if (request.TotalAmountExclVat < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmountExclVat", new[] { "TotalAmountExclVat must be >= 0." } }
                });
            }

            // Base mapping (respects your AutoMapper profile)
            _mapper.Map(request, product);

            // Totals are ignored by AutoMapper → must be reassigned
            product.TotalAmountExclVat = request.TotalAmountExclVat;
            product.TotalVatAmount = request.TotalVatAmount;
            product.TotalAmountInclVat = request.TotalAmountInclVat;

            // Audit
            product.ModifiedAt = DateTime.UtcNow;

            _repository.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResult>(product);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted)
            {
                throw new NotFoundException("Purchase", id);
            }

            product.IsDeleted = true;
            product.ModifiedAt = DateTime.UtcNow;

            _repository.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<PurchaseResult>> QueryAsync(PurchaseQuery query)
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
                    p.PurchaseNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    (p.SupplierInvoiceNumber != null && p.SupplierInvoiceNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase)));
            }

            // Status filter
            if (query.Status.HasValue)
            {
                var status = (PurchaseStatus)query.Status.Value;
                filtered = filtered.Where(p => p.Status == status);
            }

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "PurchaseNumber" => query.Desc
                    ? filtered.OrderByDescending(p => p.PurchaseNumber)
                    : filtered.OrderBy(p => p.PurchaseNumber),

                "salePrice" => query.Desc
                    ? filtered.OrderByDescending(p => p.PurchaseDate)
                    : filtered.OrderBy(p => p.PurchaseDate),

                "purchasePrice" => query.Desc
                    ? filtered.OrderByDescending(p => p.Supplier.Name)
                    : filtered.OrderBy(p => p.Supplier.Name),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<PurchaseResult>
            {
                Items = _mapper.Map<List<PurchaseResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}