using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class PurchaseService
    {
        private readonly IRepository<Purchase> _repository;
        private readonly IRepository<PurchaseLine> _purchaseLineRepository;
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IRepository<PurchasePayment> _paymentRepository;
        private readonly IRepository<Supplier> _supplierRepository;
        private readonly IRepository<SupplierTransaction> _supplierTransactionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDocumentNumberService _documentNumberService;
        private readonly ITenantContext _tenantContext;

        public PurchaseService(
            IRepository<Purchase> repository,
            IRepository<PurchaseLine> purchaseLineRepository,
            IRepository<StockMovement> stockMovementRepository,
            IRepository<Stock> stockRepository,
            IRepository<PurchasePayment> paymentRepository,
            IRepository<Supplier> supplierRepository,
            IRepository<SupplierTransaction> supplierTransactionRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDocumentNumberService documentNumberService,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _purchaseLineRepository = purchaseLineRepository;
            _stockMovementRepository = stockMovementRepository;
            _stockRepository = stockRepository;
            _paymentRepository = paymentRepository;
            _supplierRepository = supplierRepository;
            _supplierTransactionRepository = supplierTransactionRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _documentNumberService = documentNumberService;
            _tenantContext = tenantContext;
        }

        // =========================
        // CREATE (HEADER ONLY)
        // =========================
        public async Task<PurchaseResult> CreateAsync(CreatePurchaseRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (request.TotalAmountInclVat <= 0 || request.TotalAmountExclVat <= 0)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Amount", new[] { "Amounts must be greater than 0." } }
                });

            var entity = _mapper.Map<Purchase>(request);

            entity.Id = Guid.NewGuid();
            entity.TenantId = tenantId;
            entity.PurchaseNumber = await _documentNumberService.GenerateAsync("PURCHASE");
            entity.PurchaseDate = request.PurchaseDate == default ? DateTime.UtcNow : request.PurchaseDate;
            entity.Status = PurchaseStatus.Received;
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResult>(entity);
        }

        // =========================
        // CREATE COMPLETE
        // =========================
        public async Task<PurchaseResult> CreateCompleteAsync(CreateCompletePurchaseRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (request.Lines == null || !request.Lines.Any())
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Lines", new[] { "At least one purchase line is required." } }
                });

            var purchase = new Purchase
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SupplierId = request.SupplierId,
                PurchaseNumber = await _documentNumberService.GenerateAsync("PURCHASE"),
                PurchaseDate = request.PurchaseDate == default ? DateTime.UtcNow : request.PurchaseDate,
                Status = PurchaseStatus.Received,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            purchase.TotalAmountExclVat = request.Lines.Sum(l => l.LineAmountExclVat);
            purchase.TotalVatAmount = request.Lines.Sum(l => l.LineVatAmount);
            purchase.TotalAmountInclVat = request.Lines.Sum(l => l.LineAmountInclVat);

            await _repository.AddAsync(purchase);

            foreach (var lineItem in request.Lines)
            {
                var line = new PurchaseLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PurchaseId = purchase.Id,
                    ProductId = lineItem.ProductId,
                    QuantityReceived = lineItem.Quantity,
                    UnitPurchasePrice = lineItem.UnitPrice,
                    VatRate = lineItem.VatRate
                };
                await _purchaseLineRepository.AddAsync(line);

                var stock = await _stockRepository.GetSingleAsync(
                    s => s.ProductId == lineItem.ProductId &&
                         s.TenantId == tenantId &&
                         !s.IsDeleted);

                var before = stock?.Quantity ?? 0;

                if (stock == null)
                {
                    stock = new Stock
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ProductId = lineItem.ProductId,
                        Quantity = lineItem.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow
                    };
                    await _stockRepository.AddAsync(stock);
                }
                else
                {
                    stock.Quantity += lineItem.Quantity;
                    stock.LastUpdated = DateTime.UtcNow;
                    stock.ModifiedAt = DateTime.UtcNow;
                    _stockRepository.Update(stock);
                }

                await _stockMovementRepository.AddAsync(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = lineItem.ProductId,
                    Type = StockMovementType.Purchase,
                    QuantityChange = lineItem.Quantity,
                    QuantityBefore = before,
                    QuantityAfter = stock.Quantity,
                    ReferenceId = purchase.Id,
                    ReferenceNumber = purchase.PurchaseNumber,
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });
            }

            if (request.Payment != null)
            {
                Enum.TryParse<PaymentMethod>(
                    request.Payment.PaymentMethod,
                    true,
                    out var method);

                await _paymentRepository.AddAsync(new PurchasePayment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PurchaseId = purchase.Id,
                    Amount = request.Payment.Amount,
                    Method = method == 0 ? PaymentMethod.Cash : method,
                    TransactionRef = request.Payment.Reference,
                    PaymentDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });
            }

            var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId);
            if (supplier != null && !supplier.IsDeleted && supplier.TenantId == tenantId)
            {
                supplier.CurrentBalance += purchase.TotalAmountInclVat;
                supplier.ModifiedAt = DateTime.UtcNow;
                _supplierRepository.Update(supplier);

                await _supplierTransactionRepository.AddAsync(new SupplierTransaction
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SupplierId = supplier.Id,
                    Type = SupplierTransactionType.Purchase,
                    Amount = purchase.TotalAmountInclVat,
                    PurchaseId = purchase.Id,
                    ReferenceNumber = purchase.PurchaseNumber,
                    TransactionDate = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<PurchaseResult>(purchase);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<PurchaseResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Purchase", id);

            return _mapper.Map<PurchaseResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<PurchaseResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();

            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<PurchaseResult>>(
                entities.Where(x => !x.IsDeleted && x.TenantId == tenantId).ToList()
            );
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<PurchaseResult> UpdateAsync(Guid id, UpdatePurchaseRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Purchase", id);

            _mapper.Map(request, entity);
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResult>(entity);
        }

        // =========================
        // DELETE
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Purchase", id);

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<PurchaseResult>> QueryAsync(PurchaseQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            var source = (await _repository.GetAllAsync())
                .Where(x => !x.IsDeleted && x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
                source = source.Where(x => x.PurchaseNumber.Contains(query.Search));

            var total = source.Count();

            var items = source
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<PurchaseResult>
            {
                Items = _mapper.Map<List<PurchaseResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
