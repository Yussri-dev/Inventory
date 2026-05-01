using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

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
        private readonly IRepository<CashMovement> _cashMovementRepository;
        private readonly ICashSessionService _cashSessionService;
        private readonly IPackService _packService;
        private readonly IRepository<Product> _productRepository;

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
            IRepository<CashMovement> cashMovementRepository,
            ICashSessionService cashSessionService,
            IRepository<Product> productRepository,
            IPackService packService,
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
            _cashMovementRepository = cashMovementRepository;
            _cashSessionService = cashSessionService;
            _productRepository = productRepository;
            _packService = packService;
        }

        // =========================
        // CREATE (HEADER ONLY)
        // =========================
        public async Task<PurchaseResult> CreateAsync(CreatePurchaseRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            if (request == null)
                throw new ValidationException("Request cannot be null.");

            if (request.TotalAmountInclVat <= 0 || request.TotalAmountExclVat <= 0)
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Amount", new[] { "Amounts must be greater than 0." } }
        });

            // ── Supplier validation ─────────────────────────────────────
            var supplier = await _supplierRepository.GetSingleAsync(s =>
                s.Id == request.SupplierId &&
                !s.IsDeleted &&
                s.TenantId == tenantId);

            if (supplier == null)
                throw new NotFoundException("Supplier", request.SupplierId);

            var entity = _mapper.Map<Purchase>(request);

            entity.Id = Guid.NewGuid();
            entity.TenantId = tenantId;
            entity.PurchaseNumber = await _documentNumberService.GenerateAsync("PURCHASE");
            entity.PurchaseDate = request.PurchaseDate == default ? DateTime.UtcNow : request.PurchaseDate;
            entity.Status = PurchaseStatus.Pending; // FIX: header should not be "Received"
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            // ── Normalize money ─────────────────────────────────────────
            entity.TotalAmountExclVat = Math.Round(entity.TotalAmountExclVat, 2);
            entity.TotalVatAmount = Math.Round(entity.TotalVatAmount, 2);
            entity.TotalAmountInclVat = Math.Round(entity.TotalAmountInclVat, 2);

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResult>(entity);
        }

        // =========================
        // CREATE COMPLETE
        // =========================
        public async Task<PurchaseResult> CreateCompleteAsync(CreateCompletePurchaseRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var activeCashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

            if (request == null)
                throw new ValidationException("Request cannot be null.");

            if (request.Lines == null || !request.Lines.Any())
                throw new ValidationException("Purchase must contain at least one line.");

            // ── Supplier validation ─────────────────────────────────────
            var supplier = await _supplierRepository.GetSingleAsync(s =>
                s.Id == request.SupplierId &&
                !s.IsDeleted &&
                s.TenantId == tenantId);

            if (supplier == null)
                throw new NotFoundException("Supplier", request.SupplierId);

            // ── PRELOAD PRODUCTS (NO N+1) ───────────────────────────────
            var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();

            var products = await _productRepository.Query()
                .Include(p => p.CatalogProduct)
                .Where(p => productIds.Contains(p.Id) && p.TenantId == tenantId)
                .ToListAsync();

            var productMap = products.ToDictionary(p => p.Id);

            var catalogIds = products
                .Select(p => p.CatalogProductId)
                .Distinct()
                .ToList();

            // récupérer les composants des packs
            var componentCatalogIds = catalogIds
                .Where(c => _packService.IsPack(c))
                .Select(c => _packService.GetComponentCatalogId(c))
                .Where(c => c.HasValue)
                .Select(c => c.Value)
                .ToList();

            // merge
            var allCatalogIds = catalogIds
                .Concat(componentCatalogIds)
                .Distinct()
                .ToList();

            // charger tous les produits nécessaires
            var unitProducts = await _productRepository.Query()
                .Where(p => allCatalogIds.Contains(p.CatalogProductId) && p.TenantId == tenantId)
                .ToListAsync();

            var unitProductMap = unitProducts
                .GroupBy(p => p.CatalogProductId)
                .ToDictionary(g => g.Key, g => g.First());

            // ── RESOLVE LINES (PACK → UNIT) ─────────────────────────────
            var lineResolutions = new List<PurchaseLineResolution>();

            foreach (var line in request.Lines)
            {
                if (!productMap.TryGetValue(line.ProductId, out var product))
                    throw new NotFoundException("Product", line.ProductId);

                if (line.Quantity <= 0)
                    throw new ValidationException("Quantity must be > 0");

                var catalogId = product.CatalogProductId;

                if (_packService.IsPack(catalogId))
                {
                    var componentCatalogId = _packService.GetComponentCatalogId(catalogId);

                    if (!componentCatalogId.HasValue)
                        throw new ValidationException("Pack configuration invalid.");

                    if (!unitProductMap.TryGetValue(componentCatalogId.Value, out var unitProduct))
                        throw new NotFoundException("Unit product", componentCatalogId.Value);

                    lineResolutions.Add(new PurchaseLineResolution
                    {
                        OriginalLine = line,
                        StockProductId = unitProduct.Id,
                        StockQuantity = _packService.GetUnitQuantity(catalogId, line.Quantity),
                        IsPack = true,
                        PackSize = _packService.GetPackSize(catalogId)
                    });
                }
                else
                {
                    lineResolutions.Add(new PurchaseLineResolution
                    {
                        OriginalLine = line,
                        StockProductId = line.ProductId,
                        StockQuantity = line.Quantity,
                        IsPack = false,
                        PackSize = 1m
                    });
                }
            }

            // ── TOTALS (BACKEND AUTHORITATIVE) ──────────────────────────
            decimal totalExclVat = 0m;
            decimal totalVat = 0m;

            foreach (var l in request.Lines)
            {
                var excl = l.Quantity * l.UnitPrice * (1 - l.DiscountPercent / 100m);
                var vat = excl * (l.VatRate / 100m);

                totalExclVat += excl;
                totalVat += vat;
            }

            totalExclVat = Math.Round(totalExclVat, 2);
            totalVat = Math.Round(totalVat, 2);
            var totalInclVat = Math.Round(totalExclVat + totalVat, 2);

            // ── CREATE PURCHASE ─────────────────────────────────────────
            var purchase = new Purchase
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SupplierId = request.SupplierId,
                PurchaseNumber = await _documentNumberService.GenerateAsync("PURCHASE"),
                PurchaseDate = request.PurchaseDate == default ? DateTime.UtcNow : request.PurchaseDate,
                TotalAmountExclVat = totalExclVat,
                TotalVatAmount = totalVat,
                TotalAmountInclVat = totalInclVat,
                Status = PurchaseStatus.Received,
                PaymentDate = request.Payment != null ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(purchase);

            // ── LINES ───────────────────────────────────────────────────
            foreach (var line in request.Lines)
            {
                await _purchaseLineRepository.AddAsync(new PurchaseLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PurchaseId = purchase.Id,
                    ProductId = line.ProductId,
                    QuantityOrdered = line.Quantity,
                    QuantityReceived = line.Quantity,
                    UnitPurchasePrice = line.UnitPrice,
                    VatRate = line.VatRate
                });
            }

            // ── STOCK ───────────────────────────────────────────────────
            foreach (var group in lineResolutions.GroupBy(r => r.StockProductId))
            {
                var stock = await _stockRepository.GetSingleAsync(
                    s => s.ProductId == group.Key &&
                         !s.IsDeleted &&
                         s.TenantId == tenantId);

                var before = stock?.Quantity ?? 0;
                var qty = group.Sum(x => x.StockQuantity);
                var after = before + qty;

                await _stockMovementRepository.AddAsync(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = group.Key,
                    Type = StockMovementType.Purchase,
                    QuantityChange = qty,
                    QuantityBefore = before,
                    QuantityAfter = after,
                    ReferenceId = purchase.Id,
                    ReferenceNumber = purchase.PurchaseNumber,
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

                if (stock == null)
                {
                    await _stockRepository.AddAsync(new Stock
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ProductId = group.Key,
                        Quantity = after,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    stock.Quantity = after;
                    stock.ModifiedAt = DateTime.UtcNow;
                    _stockRepository.Update(stock);
                }
            }

            // ── PAYMENT ─────────────────────────────────────────────────
            decimal cashAmount = 0m;

            if (request.Payment != null)
            {
                if (!Enum.TryParse<PaymentMethod>(request.Payment.PaymentMethod, true, out var method))
                    throw new ValidationException("Invalid payment method.");

                await _paymentRepository.AddAsync(new PurchasePayment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PurchaseId = purchase.Id,
                    Method = method,
                    Amount = request.Payment.Amount,
                    TransactionRef = request.Payment.Reference,
                    PaymentDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });

                if (method == PaymentMethod.Cash)
                    cashAmount = request.Payment.Amount;
            }

            // ── CASH MOVEMENT ───────────────────────────────────────────
            if (cashAmount > 0)
            {
                var last = await _cashMovementRepository.GetLastAsync(
                    m => m.CashSessionId == activeCashSessionId &&
                         !m.IsDeleted &&
                         m.TenantId == tenantId,
                    m => m.MovementDate);

                var before = last?.BalanceAfter ?? 0m;
                var after = before - cashAmount;

                if (after < 0)
                    throw new ValidationException("Cash drawer cannot go negative.");

                await _cashMovementRepository.AddAsync(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CashSessionId = activeCashSessionId,
                    Type = CashMovementType.Withdrawal,
                    Amount = cashAmount,
                    BalanceBefore = before,
                    BalanceAfter = after,
                    ReferenceId = purchase.Id,
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResult>(purchase);
        }

        // Classe helper privée
        private class PurchaseLineResolution
        {
            public PurchaseLineItem OriginalLine { get; set; } = null!;
            public Guid StockProductId { get; set; }
            public decimal StockQuantity { get; set; }
            public bool IsPack { get; set; }
            public decimal PackSize { get; set; }
        }
        /*
        public async Task<PurchaseResult> CreateCompleteAsync(CreateCompletePurchaseRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var activeCashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

            // =========================
            // CALCULATE TOTALS (BACKEND AUTHORITATIVE)
            // =========================
            decimal totalExclVat = 0m;
            decimal totalVat = 0m;

            foreach (var l in request.Lines)
            {
                var lineExcl = l.Quantity * l.UnitPrice * (1 - l.DiscountPercent / 100);
                var lineVat = lineExcl * (l.VatRate / 100);

                totalExclVat += lineExcl;
                totalVat += lineVat;
            }

            var totalInclVat = totalExclVat + totalVat;

            // =========================
            // CREATE PURCHASE
            // =========================
            var purchase = new Purchase
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SupplierId = request.SupplierId,
                PurchaseNumber = await _documentNumberService.GenerateAsync("PURCHASE"),
                PurchaseDate = request.PurchaseDate,
                TotalAmountExclVat = totalExclVat,
                TotalVatAmount = totalVat,
                TotalAmountInclVat = totalInclVat,
                Status = PurchaseStatus.Received,
                PaymentDate = request.Payment != null ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(purchase);

            // =========================
            // LINES + STOCK
            // =========================
            foreach (var line in request.Lines)
            {
                var purchaseLine = new PurchaseLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PurchaseId = purchase.Id,
                    ProductId = line.ProductId,
                    QuantityOrdered = line.Quantity,
                    QuantityReceived = line.Quantity,
                    UnitPurchasePrice = line.UnitPrice,
                    VatRate = line.VatRate
                };

                await _purchaseLineRepository.AddAsync(purchaseLine);

                var stock = await _stockRepository.GetSingleAsync(
                    s => s.ProductId == line.ProductId &&
                         !s.IsDeleted &&
                         s.TenantId == tenantId);

                var quantityBefore = stock?.Quantity ?? 0;
                var quantityAfter = quantityBefore + line.Quantity;

                await _stockMovementRepository.AddAsync(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = line.ProductId,
                    Type = StockMovementType.Purchase,
                    QuantityChange = line.Quantity,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = quantityAfter,
                    ReferenceId = purchase.Id,
                    ReferenceNumber = purchase.PurchaseNumber,
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });

                if (stock == null)
                {
                    await _stockRepository.AddAsync(new Stock
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ProductId = line.ProductId,
                        Quantity = quantityAfter,
                        LastUpdated = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    stock.Quantity = quantityAfter;
                    stock.LastUpdated = DateTime.UtcNow;
                    stock.ModifiedAt = DateTime.UtcNow;
                    _stockRepository.Update(stock);
                }
            }

            // =========================
            // PAYMENT
            // =========================
            decimal cashAmount = 0m;

            if (request.Payment != null)
            {
                Enum.TryParse<PaymentMethod>(request.Payment.PaymentMethod, true, out var method);

                await _paymentRepository.AddAsync(new PurchasePayment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PurchaseId = purchase.Id,
                    Method = method,
                    Amount = request.Payment.Amount,
                    TransactionRef = request.Payment.Reference,
                    PaymentDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });

                if (method == PaymentMethod.Cash)
                    cashAmount = request.Payment.Amount;
            }

            // =========================
            // CASH MOVEMENT — CASH OUT
            // =========================
            if (cashAmount > 0)
            {
                var last = await _cashMovementRepository.GetLastAsync(
                    m => m.CashSessionId == activeCashSessionId &&
                         !m.IsDeleted &&
                         m.TenantId == tenantId,
                    m => m.MovementDate
                );

                var before = last?.BalanceAfter ?? 0m;
                var after = before - cashAmount;

                if (after < 0)
                    throw new ValidationException("Cash drawer cannot go negative.");

                await _cashMovementRepository.AddAsync(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CashSessionId = activeCashSessionId,
                    Type = CashMovementType.Withdrawal,
                    Amount = cashAmount,
                    BalanceBefore = before,
                    BalanceAfter = after,
                    Reason = $"Purchase {purchase.PurchaseNumber}",
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResult>(purchase);
        }
        */
        // =========================
        // GET BY ID
        // =========================
        public async Task<PurchaseResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Purchase", id);

            return _mapper.Map<PurchaseResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        //public async Task<List<PurchaseResult>> GetAllAsync()
        //{
        //    var tenantId = _tenantContext.TenantId;

        //    var entities = await _repository.GetAllAsync();
        //    return _mapper.Map<List<PurchaseResult>>(
        //        entities.Where(x => !x.IsDeleted && x.TenantId == tenantId).ToList()
        //    );
        //}

        public async Task<List<PurchaseResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;
            var purchases = await _repository.GetAsync(
                p => !p.IsDeleted && p.TenantId == tenantId);

            return _mapper.Map<List<PurchaseResult>>(purchases);
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<PurchaseResult> UpdateAsync(Guid id, UpdatePurchaseRequest request)
        {
            var tenantId = _tenantContext.TenantId;
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
            var tenantId = _tenantContext.TenantId;
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
        //public async Task<PagedResult<PurchaseResult>> QueryAsync(PurchaseQuery query)
        //{
        //    var tenantId = _tenantContext.TenantId;

        //    var source = (await _repository.GetAllAsync())
        //        .Where(x => !x.IsDeleted && x.TenantId == tenantId)
        //        .AsQueryable();

        //    if (!string.IsNullOrWhiteSpace(query.Search))
        //        source = source.Where(x => x.PurchaseNumber.Contains(query.Search));

        //    var total = source.Count();

        //    var items = source
        //        .Skip((query.Page - 1) * query.PageSize)
        //        .Take(query.PageSize)
        //        .ToList();

        //    return new PagedResult<PurchaseResult>
        //    {
        //        Items = _mapper.Map<List<PurchaseResult>>(items),
        //        TotalCount = total,
        //        Page = query.Page,
        //        PageSize = query.PageSize
        //    };
        //}

        public async Task<PagedResult<PurchaseResult>> QueryAsync(PurchaseQuery query)
        {
            var tenantId = _tenantContext.TenantId;
            var search = query.Search?.Trim() ?? string.Empty;

            var all = _repository.Query()
                .Where(p => !p.IsDeleted
                && p.TenantId == tenantId
                && p.PurchaseNumber.Contains(search));

            var total = await all.CountAsync();

            var items = await all
                .OrderByDescending(p => p.PurchaseDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

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
