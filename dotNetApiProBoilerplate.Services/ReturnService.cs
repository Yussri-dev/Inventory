using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Returns.Requests;
using Inventory.Dto.Returns.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class ReturnService
    {
        private readonly IRepository<Return> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReturnService(
            IRepository<Return> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CREATE
        // CREATE
        public async Task<ReturnResult> CreateAsync(CreateReturnRequest request)
        {
            var exists = await _repository.ExistsAsync(
                p => p.ReturnNumber == request.ReturnNumber && !p.IsDeleted);

            if (exists)
            {
                throw new ConflictException(
                    $"Return with purchase number '{request.ReturnNumber}' already exists.");

            }

            if (request.TotalAmount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmount", new[] { "TotalAmount must be > 0." } }
                });

            }

            var product = _mapper.Map<Return>(request);

            product.Id = Guid.NewGuid();

            product.ReturnDate = request.ReturnDate == default
                ? DateTime.UtcNow
                : request.ReturnDate;

            product.TotalAmount = request.TotalAmount;

            product.CreatedAt = DateTime.UtcNow;
            product.ModifiedAt = DateTime.UtcNow;
            // =========================

            await _repository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReturnResult>(product);
        }

        // GET BY ID
        public async Task<ReturnResult> GetByIdAsync(Guid id)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted)
            {
                throw new NotFoundException("Return", id);
            }

            return _mapper.Map<ReturnResult>(product);
        }

        // GET ALL
        public async Task<List<ReturnResult>> GetAllAsync()
        {
            var products = await _repository.GetAllAsync();

            // Filter out soft-deleted products
            var activeReturns = products.Where(p => !p.IsDeleted).ToList();

            return _mapper.Map<List<ReturnResult>>(activeReturns);
        }

        // UPDATE
        public async Task<ReturnResult> UpdateAsync(Guid id, UpdateReturnRequest request)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted)
            {
                throw new NotFoundException("Return", id);
            }

            // Uniqueness check
            if (!string.IsNullOrWhiteSpace(request.ReturnNumber) &&
                request.ReturnNumber != product.ReturnNumber)
            {
                var nameExists = await _repository.ExistsAsync(p =>
                    p.ReturnNumber == request.ReturnNumber &&
                    p.Id != id &&
                    !p.IsDeleted);

                if (nameExists)
                {
                    throw new ConflictException(
                        $"Return with number '{request.ReturnNumber}' already exists.");
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

            return _mapper.Map<ReturnResult>(product);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null || product.IsDeleted)
            {
                throw new NotFoundException("Return", id);
            }

            product.IsDeleted = true;
            product.ModifiedAt = DateTime.UtcNow;

            _repository.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<ReturnResult>> QueryAsync(ReturnQuery query)
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
                    p.ReturnNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    (p.ReturnNumber != null && p.ReturnNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase)));
            }

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "ReturnNumber" => query.Desc
                    ? filtered.OrderByDescending(p => p.ReturnNumber)
                    : filtered.OrderBy(p => p.ReturnNumber),

                "ReturnDate" => query.Desc
                    ? filtered.OrderByDescending(p => p.ReturnDate)
                    : filtered.OrderBy(p => p.ReturnDate),

                "InvoiceNumber" => query.Desc
                    ? filtered.OrderByDescending(p => p.Sale.InvoiceNumber)
                    : filtered.OrderBy(p => p.Sale.InvoiceNumber),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<ReturnResult>
            {
                Items = _mapper.Map<List<ReturnResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}