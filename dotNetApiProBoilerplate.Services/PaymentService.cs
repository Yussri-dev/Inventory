using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Payments.Requests;
using Inventory.Dto.Payments.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;
using Inventory.Services.Context;

namespace Inventory.Services
{
    public class PaymentService
    {
        private readonly IRepository<Payment> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;
        public PaymentService(
            IRepository<Payment> repository,
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
        public async Task<PaymentResult> CreateAsync(CreatePaymentRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;


            var exists = await _repository.ExistsAsync(c =>
            c.TransactionRef == request.TransactionRef && !c.IsDeleted && c.TenantId != tenantId);
           
            if (exists)
            {
                throw new ConflictException($"Payment with Transaction ref '{request.TransactionRef}' already exists.");
            }

            if (request.TransactionRef.Length == 0)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Name", new[] { "Payment name must not be empty." } }
                };
            }

            var customer = _mapper.Map<Payment>(request);

            customer.Id = Guid.NewGuid();
            customer.CreatedAt = DateTime.UtcNow;
            customer.ModifiedAt = DateTime.UtcNow;
            customer.TenantId = tenantId;
            customer.CreatedByUserId = userId;
            await _repository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<PaymentResult>(customer);
        }


        //GET BY ID
        public async Task<PaymentResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var customer = await _repository.GetByIdAsync(id);

            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
            {
                throw new NotFoundException("Payment", id);
            }

            return _mapper.Map<PaymentResult>(customer);
        }

        //GET ALL
        public async Task<List<PaymentResult>> GetAllAsync()
        {
            var customers = await _repository.GetAllAsync();

            var activePayments = customers.Where(c => !c.IsDeleted).ToList();

            return _mapper.Map<List<PaymentResult>>(activePayments);
        }

        //UPDATE
        public async Task<PaymentResult> UpdateAsync(Guid id, UpdatePaymentRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var customer = await _repository.GetByIdAsync(id);
            
            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
            {
                throw new NotFoundException("Payment", id);
            }

            if (!string.IsNullOrWhiteSpace(request.TransactionRef) && request.TransactionRef != customer.TransactionRef)
            {
                var nameExists = await _repository.ExistsAsync(
                    c => c.TransactionRef == request.TransactionRef && c.Id != id && !c.IsDeleted);
                if (nameExists)
                {
                    throw new ConflictException($"Payment with transaction ref '{request.TransactionRef}' already exists.");
                }

                if (request.TransactionRef.Length == 0)
                {
                    var errors = new Dictionary<string, string[]>
                    {
                        { "Name", new[] { "Payment name must not be empty." } }
                    };
                    throw new ValidationException(errors);
                }
            }

            _mapper.Map(request, customer);

            customer.ModifiedAt = DateTime.UtcNow;
            customer.ModifiedByUserId = userId;

            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PaymentResult>(customer);
        }

        //DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var customer = await _repository.GetByIdAsync(id);
            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
            {
                throw new NotFoundException("Payment", id);
            }
            customer.IsDeleted = true;
            customer.ModifiedAt = DateTime.UtcNow;
            customer.ModifiedByUserId= userId;

            _repository.Update(customer);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // Pagination + filtering + sorting
        public async Task<PagedResult<PaymentResult>> QueryAsync(PaymentQuery query)
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
                    p.TransactionRef.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
            }

            // Sorting
            filtered = query.SortBy?.ToLower() switch
            {
                "ref" => query.Desc
                    ? filtered.OrderByDescending(p => p.TransactionRef)
                    : filtered.OrderBy(p => p.TransactionRef),

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

            return new PagedResult<PaymentResult>
            {
                Items = _mapper.Map<List<PaymentResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
