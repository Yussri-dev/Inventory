using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CustomerTransactionService
    {
        private readonly IRepository<CustomerTransaction> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CustomerTransactionService(
            IRepository<CustomerTransaction> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        //CREATE
        public async Task<CustomerTransactionResult> CreateAsync(CreateCustomerTransactionRequest request)
        {
            var customer = _mapper.Map<CustomerTransaction>(request);

            customer.Id = Guid.NewGuid();
            customer.CreatedAt = DateTime.UtcNow;
            customer.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CustomerTransactionResult>(customer);
        }


        //GET BY ID
        public async Task<CustomerTransactionResult> GetByIdAsync(Guid id)
        {
            var customer = await _repository.GetByIdAsync(id);

            if (customer == null || customer.IsDeleted)
            {
                throw new NotFoundException("CustomerTransaction", id);
            }

            return _mapper.Map<CustomerTransactionResult>(customer);
        }

        //GET ALL
        public async Task<List<CustomerTransactionResult>> GetAllAsync()
        {
            var customers = await _repository.GetAllAsync();

            var activeCustomerTransactions = customers.Where(c => !c.IsDeleted).ToList();

            return _mapper.Map<List<CustomerTransactionResult>>(activeCustomerTransactions);
        }

        //UPDATE
        public async Task<CustomerTransactionResult> UpdateAsync(Guid id, UpdateCustomerTransactionRequest request)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer == null || customer.IsDeleted)
            {
                throw new NotFoundException("CustomerTransaction", id);
            }

            _mapper.Map(request, customer);

            customer.ModifiedAt = DateTime.UtcNow;

            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CustomerTransactionResult>(customer);
        }

        //DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer == null || customer.IsDeleted)
            {
                throw new NotFoundException("CustomerTransaction", id);
            }
            customer.IsDeleted = true;
            customer.ModifiedAt = DateTime.UtcNow;
            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // Pagination + filtering + sorting
        public async Task<PagedResult<CustomerTransactionResult>> QueryAsync(CustomerTransactionQuery query)
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

            filtered = query.SortBy?.ToLower() switch
            {
                "name" => query.Desc
                    ? filtered.OrderByDescending(p => p.TransactionDate)
                    : filtered.OrderBy(p => p.TransactionDate),

                "CurrentBalance" => query.Desc
                    ? filtered.OrderByDescending(p => p.Type)
                    : filtered.OrderBy(p => p.Type),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<CustomerTransactionResult>
            {
                Items = _mapper.Map<List<CustomerTransactionResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
