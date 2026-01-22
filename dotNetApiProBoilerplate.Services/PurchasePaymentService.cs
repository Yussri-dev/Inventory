using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.PurchasePayments.Requests;
using Inventory.Dto.PurchasePayments.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class PurchasePaymentService
    {
        private readonly IRepository<PurchasePayment> _repository;
        private readonly IRepository<Purchase> _purchaseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public PurchasePaymentService(
            IRepository<PurchasePayment> repository,
            IRepository<Purchase> purchaseRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _purchaseRepository = purchaseRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // CREATE
        public async Task<PurchasePaymentResult> CreateAsync(CreatePurchasePaymentRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            if (request.Amount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Amount", new[] { "Amount must be > 0." } }
                });
            }

            // Verify purchase belongs to tenant
            var purchase = await _purchaseRepository.GetByIdAsync(request.PurchaseId);
            if (purchase == null || purchase.TenantId != tenantId)
            {
                throw new NotFoundException("Purchase", request.PurchaseId);
            }

            var purchasePayment = _mapper.Map<PurchasePayment>(request);
            purchasePayment.Id = Guid.NewGuid();
            purchasePayment.TenantId = tenantId;
            purchasePayment.CreatedByUserId = userId;
            purchasePayment.PaymentDate = DateTime.UtcNow;
            purchasePayment.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(purchasePayment);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchasePaymentResult>(purchasePayment);
        }

        // GET BY ID
        public async Task<PurchasePaymentResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var purchasePayment = await _repository.GetByIdAsync(id);

            if (purchasePayment == null || purchasePayment.TenantId != tenantId)
            {
                throw new NotFoundException("PurchasePayment", id);
            }

            return _mapper.Map<PurchasePaymentResult>(purchasePayment);
        }

        // GET ALL
        public async Task<List<PurchasePaymentResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;
            var purchasePayments = await _repository.GetAllAsync();

            var filtered = purchasePayments
                .Where(pp => pp.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<PurchasePaymentResult>>(filtered);
        }

        // UPDATE
        public async Task<PurchasePaymentResult> UpdateAsync(Guid id, UpdatePurchasePaymentRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            if (request.Amount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Amount", new[] { "Amount must be > 0." } }
                });
            }

            var purchasePayment = await _repository.GetByIdAsync(id);
            if (purchasePayment == null || purchasePayment.TenantId != tenantId)
            {
                throw new NotFoundException("PurchasePayment", id);
            }

            _mapper.Map(request, purchasePayment);
            purchasePayment.ModifiedAt = DateTime.UtcNow;
            purchasePayment.ModifiedByUserId = userId;

            _repository.Update(purchasePayment);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchasePaymentResult>(purchasePayment);
        }

        // DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var purchasePayment = await _repository.GetByIdAsync(id);
            if (purchasePayment == null || purchasePayment.TenantId != tenantId)
            {
                throw new NotFoundException("PurchasePayment", id);
            }

            purchasePayment.IsDeleted = true;
            purchasePayment.DeletedAt = DateTime.UtcNow;
            purchasePayment.DeletedByUserId = userId;

            _repository.Update(purchasePayment);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // QUERY
        public async Task<PagedResult<PurchasePaymentResult>> QueryAsync(PurchasePaymentQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                });
            }

            var filtered = (await _repository.GetAllAsync())
                .Where(pp => pp.TenantId == tenantId && !pp.IsDeleted)
                .AsQueryable();

            var total = filtered.Count();
            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<PurchasePaymentResult>
            {
                Items = _mapper.Map<List<PurchasePaymentResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}