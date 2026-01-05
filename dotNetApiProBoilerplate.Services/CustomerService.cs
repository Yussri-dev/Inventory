using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CustomerService
    {
        private readonly IRepository<Customer> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public CustomerService(
            IRepository<Customer> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        //CREATE
        public async Task<CustomerResult> CreateAsync(CreateCustomerRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            // Check if customer exists within the same tenant
            var exists = await _repository.ExistsAsync(c =>
                c.Name == request.Name &&
                c.TenantId == tenantId &&
                !c.IsDeleted);

            if (exists)
            {
                throw new ConflictException($"Customer with name '{request.Name}' already exists.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Name", new[] { "Customer name must not be empty." } }
                };
                throw new ValidationException(errors);
            }

            var customer = _mapper.Map<Customer>(request);

            customer.Id = Guid.NewGuid();
            customer.TenantId = tenantId;  // ✅ Set tenant ID
            customer.CreatedByUserId = userId;  // ✅ Set creator
            customer.IsActive = true;
            customer.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CustomerResult>(customer);
        }

        //GET BY ID
        public async Task<CustomerResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();

            var customer = await _repository.GetByIdAsync(id);

            // Ensure customer belongs to the current tenant
            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
            {
                throw new NotFoundException("Customer", id);
            }

            return _mapper.Map<CustomerResult>(customer);
        }

        //GET ALL
        public async Task<List<CustomerResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();

            var customers = await _repository.GetAllAsync();

            // Only return customers from the current tenant
            var activeCustomers = customers
                .Where(c => !c.IsDeleted && c.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<CustomerResult>>(activeCustomers);
        }

        //UPDATE
        public async Task<CustomerResult> UpdateAsync(Guid id, UpdateCustomerRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var customer = await _repository.GetByIdAsync(id);

            // Ensure customer belongs to the current tenant
            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
            {
                throw new NotFoundException("Customer", id);
            }

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != customer.Name)
            {
                var nameExists = await _repository.ExistsAsync(
                    c => c.Name == request.Name &&
                         c.Id != id &&
                         c.TenantId == tenantId &&
                         !c.IsDeleted);

                if (nameExists)
                {
                    throw new ConflictException($"Customer with name '{request.Name}' already exists.");
                }
            }

            _mapper.Map(request, customer);

            customer.ModifiedAt = DateTime.UtcNow;
            customer.ModifiedByUserId = userId;  

            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CustomerResult>(customer);
        }

        //DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var customer = await _repository.GetByIdAsync(id);

            // Ensure customer belongs to the current tenant
            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
            {
                throw new NotFoundException("Customer", id);
            }

            customer.IsDeleted = true;
            customer.DeletedAt = DateTime.UtcNow;
            customer.DeletedByUserId = userId;  // ✅ Track who deleted

            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // Pagination + filtering + sorting
        public async Task<PagedResult<CustomerResult>> QueryAsync(CustomerQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

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

            // Filter: only current tenant, not deleted
            var filtered = all
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                filtered = filtered.Where(p =>
                    p.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
            }

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "name" => query.Desc
                    ? filtered.OrderByDescending(p => p.Name)
                    : filtered.OrderBy(p => p.Name),

                "currentbalance" => query.Desc
                    ? filtered.OrderByDescending(p => p.CurrentBalance)
                    : filtered.OrderBy(p => p.CurrentBalance),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<CustomerResult>
            {
                Items = _mapper.Map<List<CustomerResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}