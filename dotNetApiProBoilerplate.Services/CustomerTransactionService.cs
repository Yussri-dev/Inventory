using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CustomerTransactionService
    {
        private readonly IRepository<CustomerTransaction> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public CustomerTransactionService(
            IRepository<CustomerTransaction> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        public async Task<CustomerTransactionResult> CreateAsync(CreateCustomerTransactionRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var transaction = _mapper.Map<CustomerTransaction>(request);

            transaction.Id = Guid.NewGuid();
            transaction.TenantId = tenantId;
            transaction.CreatedByUserId = userId;
            transaction.TransactionDate = DateTime.UtcNow;
            transaction.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CustomerTransactionResult>(transaction);
        }

        public async Task<CustomerTransactionResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var transaction = await _repository.GetByIdAsync(id);

            if (transaction == null || transaction.IsDeleted || transaction.TenantId != tenantId)
            {
                throw new NotFoundException("CustomerTransaction", id);
            }

            return _mapper.Map<CustomerTransactionResult>(transaction);
        }

        public async Task<List<CustomerTransactionResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var transactions = await _repository.GetAllAsync();

            var activeTransactions = transactions
                .Where(c => !c.IsDeleted && c.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<CustomerTransactionResult>>(activeTransactions);
        }

        public async Task<CustomerTransactionResult> UpdateAsync(Guid id, UpdateCustomerTransactionRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null || transaction.IsDeleted || transaction.TenantId != tenantId)
            {
                throw new NotFoundException("CustomerTransaction", id);
            }

            _mapper.Map(request, transaction);
            transaction.ModifiedAt = DateTime.UtcNow;
            transaction.ModifiedByUserId = userId;

            _repository.Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CustomerTransactionResult>(transaction);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var userId = _tenantContext.GetUserId();

            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null || transaction.IsDeleted || transaction.TenantId != tenantId)
            {
                throw new NotFoundException("CustomerTransaction", id);
            }

            transaction.IsDeleted = true;
            transaction.DeletedAt = DateTime.UtcNow;
            transaction.DeletedByUserId = userId;
            transaction.ModifiedAt = DateTime.UtcNow;

            _repository.Update(transaction);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<CustomerTransactionResult>> QueryAsync(CustomerTransactionQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                });
            }

            var all = await _repository.GetAllAsync();
            var filtered = all
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .AsQueryable();

            filtered = query.SortBy?.ToLower() switch
            {
                "transactiondate" => query.Desc
                    ? filtered.OrderByDescending(p => p.TransactionDate)
                    : filtered.OrderBy(p => p.TransactionDate),
                "type" => query.Desc
                    ? filtered.OrderByDescending(p => p.Type)
                    : filtered.OrderBy(p => p.Type),
                "amount" => query.Desc
                    ? filtered.OrderByDescending(p => p.Amount)
                    : filtered.OrderBy(p => p.Amount),
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