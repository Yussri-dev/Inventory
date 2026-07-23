using AutoMapper;
using AutoMapper.QueryableExtensions;
using Inventory.Domain.Entities;
using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

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
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

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
            var tenantId = _tenantContext.TenantId;

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
            var tenantId = _tenantContext.TenantId;

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
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

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
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

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
        public async Task<PagedResult<CustomerResult>> QueryAsync(
    CustomerQuery query,
    CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var tenantId =
                _tenantContext.TenantId;

            if (query.Page < 1)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                {
                    nameof(query.Page),
                    new[]
                    {
                        "Page must be greater than or equal to 1."
                    }
                }
                    });
            }

            if (query.PageSize < 1 ||
                query.PageSize > 100)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                {
                    nameof(query.PageSize),
                    new[]
                    {
                        "PageSize must be between 1 and 100."
                    }
                }
                    });
            }

            var customers =
                _repository.Query()
                    .AsNoTracking()
                    .Where(customer =>
                        customer.TenantId == tenantId &&
                        !customer.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search =
                    query.Search.Trim();

                customers =
                    customers.Where(customer =>
                        EF.Functions.ILike(
                            customer.Name,
                            $"%{search}%") ||

                        customer.Email != null &&
                        EF.Functions.ILike(
                            customer.Email,
                            $"%{search}%") ||

                        customer.Phone != null &&
                        EF.Functions.ILike(
                            customer.Phone,
                            $"%{search}%"));
            }

            customers =
                query.SortBy?
                    .Trim()
                    .ToLowerInvariant() switch
                {
                    "name" =>
                        query.Desc
                            ? customers.OrderByDescending(
                                customer => customer.Name)
                            : customers.OrderBy(
                                customer => customer.Name),

                    "currentbalance" =>
                        query.Desc
                            ? customers.OrderByDescending(
                                customer =>
                                    customer.CurrentBalance)
                            : customers.OrderBy(
                                customer =>
                                    customer.CurrentBalance),

                    _ =>
                        query.Desc
                            ? customers.OrderByDescending(
                                customer =>
                                    customer.CreatedAt)
                            : customers.OrderBy(
                                customer =>
                                    customer.CreatedAt)
                };

            var total =
                await customers.CountAsync(
                    cancellationToken);

            var items =
                await customers
                    .Skip(
                        (query.Page - 1) *
                        query.PageSize)
                    .Take(query.PageSize)
                    .ProjectTo<CustomerResult>(
                        _mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

            return new PagedResult<CustomerResult>
            {
                Items = items,
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}