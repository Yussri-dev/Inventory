using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.PurchaseLines.Requests;
using Inventory.Dto.PurchaseLines.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class PurchaseLineService
    {
        private readonly IRepository<PurchaseLine> _repository;
        private readonly IRepository<Purchase> _purchaseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public PurchaseLineService(
            IRepository<PurchaseLine> repository,
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
        public async Task<PurchaseLineResult> CreateAsync(CreatePurchaseLineRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            if (request.QuantityOrdered <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "QuantityOrdered", new[] { "QuantityOrdered must be > 0." } }
                });
            }

            // Verify purchase belongs to tenant
            var purchase = await _purchaseRepository.GetByIdAsync(request.PurchaseId);
            if (purchase == null || purchase.TenantId != tenantId)
            {
                throw new NotFoundException("Purchase", request.PurchaseId);
            }

            var purchaseLine = _mapper.Map<PurchaseLine>(request);
            purchaseLine.Id = Guid.NewGuid();

            await _repository.AddAsync(purchaseLine);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseLineResult>(purchaseLine);
        }

        // GET BY ID
        public async Task<PurchaseLineResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var purchaseLine = await _repository.GetByIdAsync(id);

            if (purchaseLine == null)
            {
                throw new NotFoundException("PurchaseLine", id);
            }

            // Verify through parent Purchase
            var purchase = await _purchaseRepository.GetByIdAsync(purchaseLine.PurchaseId);
            if (purchase == null || purchase.TenantId != tenantId)
            {
                throw new NotFoundException("PurchaseLine", id);
            }

            return _mapper.Map<PurchaseLineResult>(purchaseLine);
        }

        // GET ALL
        public async Task<List<PurchaseLineResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;
            var purchaseLines = await _repository.GetAllAsync();
            var purchases = await _purchaseRepository.GetAllAsync();

            var tenantPurchaseIds = purchases
                .Where(p => p.TenantId == tenantId)
                .Select(p => p.Id)
                .ToHashSet();

            var filteredLines = purchaseLines
                .Where(pl => tenantPurchaseIds.Contains(pl.PurchaseId))
                .ToList();

            return _mapper.Map<List<PurchaseLineResult>>(filteredLines);
        }

        // UPDATE
        public async Task<PurchaseLineResult> UpdateAsync(Guid id, UpdatePurchaseLineRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            if (request.QuantityOrdered <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "QuantityOrdered", new[] { "QuantityOrdered must be > 0." } }
                });
            }

            var purchaseLine = await _repository.GetByIdAsync(id);
            if (purchaseLine == null)
            {
                throw new NotFoundException("PurchaseLine", id);
            }

            // Verify through parent Purchase
            var purchase = await _purchaseRepository.GetByIdAsync(purchaseLine.PurchaseId);
            if (purchase == null || purchase.TenantId != tenantId)
            {
                throw new NotFoundException("PurchaseLine", id);
            }

            _mapper.Map(request, purchaseLine);
            _repository.Update(purchaseLine);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseLineResult>(purchaseLine);
        }

        // DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var purchaseLine = await _repository.GetByIdAsync(id);

            if (purchaseLine == null)
            {
                throw new NotFoundException("PurchaseLine", id);
            }

            // Verify through parent Purchase
            var purchase = await _purchaseRepository.GetByIdAsync(purchaseLine.PurchaseId);
            if (purchase == null || purchase.TenantId != tenantId)
            {
                throw new NotFoundException("PurchaseLine", id);
            }

            _repository.Delete(purchaseLine);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // QUERY
        public async Task<PagedResult<PurchaseLineResult>> QueryAsync(PurchaseLineQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                });
            }

            var purchaseLines = await _repository.GetAllAsync();
            var purchases = await _purchaseRepository.GetAllAsync();

            var tenantPurchaseIds = purchases
                .Where(p => p.TenantId == tenantId)
                .Select(p => p.Id)
                .ToHashSet();

            var filtered = purchaseLines
                .Where(pl => tenantPurchaseIds.Contains(pl.PurchaseId))
                .AsQueryable();

            var total = filtered.Count();
            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<PurchaseLineResult>
            {
                Items = _mapper.Map<List<PurchaseLineResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}