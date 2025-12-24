using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services
{
    public class CustomerService
    {
        private readonly IRepository<Customer> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CustomerService(
            IRepository<Customer> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        //CREATE
        public async Task<CustomerResult> CreateAsync(CreateCustomerRequest request)
        {
            var exists = await _repository.ExistsAsync(c => c.Name == request.Name && !c.IsDeleted);
            if (exists)
            {
                throw new ConflictException($"Customer with name '{request.Name}' already exists.");
            }

            if (request.Name.Length == 0)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Name", new[] { "Customer name must not be empty." } }
                };
            }

            var customer = _mapper.Map<Customer>(request);

            customer.Id = Guid.NewGuid();
            customer.IsActive = true;
            customer.CreatedAt = DateTime.UtcNow;
            customer.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CustomerResult>(customer);
        }


        //GET BY ID
        public async Task<CustomerResult> GetByIdAsync(Guid id)
        {
            var customer = await _repository.GetByIdAsync(id);

            if (customer == null || customer.IsDeleted)
            {
                throw new NotFoundException("Customer", id);
            }

            return _mapper.Map<CustomerResult>(customer);
        }

        //GET ALL
        public async Task<List<CustomerResult>> GetAllAsync()
        {
            var customers = await _repository.GetAllAsync();

            var activeCustomers = customers.Where(c => !c.IsDeleted).ToList();

            return _mapper.Map<List<CustomerResult>>(activeCustomers);
        }

        //UPDATE
        public async Task<CustomerResult> UpdateAsync(Guid id, UpdateCustomerRequest request)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer == null || customer.IsDeleted)
            {
                throw new NotFoundException("Customer", id);
            }

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != customer.Name)
            {
                var nameExists = await _repository.ExistsAsync(
                    c => c.Name == request.Name && c.Id != id && !c.IsDeleted);
                if (nameExists)
                {
                    throw new ConflictException($"Customer with name '{request.Name}' already exists.");
                }

                if (request.Name.Length == 0)
                {
                    var errors = new Dictionary<string, string[]>
                    {
                        { "Name", new[] { "Customer name must not be empty." } }
                    };
                    throw new ValidationException(errors);
                }
            }

            _mapper.Map(request, customer);

            customer.ModifiedAt = DateTime.UtcNow;

            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CustomerResult>(customer);
        }

        //DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer == null || customer.IsDeleted)
            {
                throw new NotFoundException("Customer", id);
            }
            customer.IsDeleted = true;
            customer.ModifiedAt = DateTime.UtcNow;
            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // Pagination + filtering + sorting
        public async Task<PagedResult<CustomerResult>> QueryAsync(CustomerQuery query)
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
                "name" => query.Desc
                    ? filtered.OrderByDescending(p => p.Name)
                    : filtered.OrderBy(p => p.Name),

                "CurrentBalance" => query.Desc
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
