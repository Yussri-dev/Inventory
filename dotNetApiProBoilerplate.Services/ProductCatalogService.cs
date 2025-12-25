using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class ProductCatalogService
    {
        private readonly IRepository<ProductCatalog> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductCatalogService(
            IRepository<ProductCatalog> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        //CREATE
        public async Task<ProductCatalogResult> CreateAsync(CreateProductCatalogRequest request)
        {
            var exists = await _repository.ExistsAsync(c => c.Name == request.Name && !c.IsDeleted);
            if (exists)
            {
                throw new ConflictException($"ProductCatalog with name '{request.Name}' already exists.");
            }

            if (request.Name.Length == 0)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Name", new[] { "ProductCatalog name must not be empty." } }
                };
            }

            var customer = _mapper.Map<ProductCatalog>(request);

            customer.Id = Guid.NewGuid();
            customer.CreatedAt = DateTime.UtcNow;
            customer.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ProductCatalogResult>(customer);
        }


        //GET BY ID
        public async Task<ProductCatalogResult> GetByIdAsync(Guid id)
        {
            var customer = await _repository.GetByIdAsync(id);

            if (customer == null || customer.IsDeleted)
            {
                throw new NotFoundException("ProductCatalog", id);
            }

            return _mapper.Map<ProductCatalogResult>(customer);
        }

        //GET ALL
        public async Task<List<ProductCatalogResult>> GetAllAsync()
        {
            var customers = await _repository.GetAllAsync();

            var activeProductCatalogs = customers.Where(c => !c.IsDeleted).ToList();

            return _mapper.Map<List<ProductCatalogResult>>(activeProductCatalogs);
        }

        //UPDATE
        public async Task<ProductCatalogResult> UpdateAsync(Guid id, UpdateProductCatalogRequest request)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer == null || customer.IsDeleted)
            {
                throw new NotFoundException("ProductCatalog", id);
            }

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != customer.Name)
            {
                var nameExists = await _repository.ExistsAsync(
                    c => c.Name == request.Name && c.Id != id && !c.IsDeleted);
                if (nameExists)
                {
                    throw new ConflictException($"ProductCatalog with name '{request.Name}' already exists.");
                }

                if (request.Name.Length == 0)
                {
                    var errors = new Dictionary<string, string[]>
                    {
                        { "Name", new[] { "ProductCatalog name must not be empty." } }
                    };
                    throw new ValidationException(errors);
                }
            }

            _mapper.Map(request, customer);

            customer.ModifiedAt = DateTime.UtcNow;

            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductCatalogResult>(customer);
        }

        //DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer == null || customer.IsDeleted)
            {
                throw new NotFoundException("ProductCatalog", id);
            }
            customer.IsDeleted = true;
            customer.ModifiedAt = DateTime.UtcNow;
            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // Pagination + filtering + sorting
        public async Task<PagedResult<ProductCatalogResult>> QueryAsync(ProductCatalogQuery query)
        {
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
                    p.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
            }

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "ref" => query.Desc
                    ? filtered.OrderByDescending(p => p.Name)
                    : filtered.OrderBy(p => p.Name),

                "amount" => query.Desc
                    ? filtered.OrderByDescending(p => p.Manufacturer)
                    : filtered.OrderBy(p => p.Manufacturer),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<ProductCatalogResult>
            {
                Items = _mapper.Map<List<ProductCatalogResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
