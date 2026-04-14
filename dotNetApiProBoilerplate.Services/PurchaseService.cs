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
            var tenantId = _tenantContext.TenantId;
            var activeCashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

            // ── Résolution des lignes (pack → unité) ─────────────────────────────────
            var lineResolutions = new List<PurchaseLineResolution>();

            foreach (var line in request.Lines)
            {
                var product = await _productRepository.Query()
                    .Include(p => p.CatalogProduct)
                    .FirstOrDefaultAsync(p =>
                        p.Id == line.ProductId &&
                        p.TenantId == tenantId);

                if (product == null)
                    throw new NotFoundException("Product", line.ProductId);

                var catalogId = product.CatalogProductId;

                if (_packService.IsPack(catalogId))
                {
                    var componentCatalogId = _packService.GetComponentCatalogId(catalogId);
                    var unitQuantity = _packService.GetUnitQuantity(catalogId, line.Quantity);
                    var packSize = _packService.GetPackSize(catalogId);

                    // Trouver le Product de l'unité
                    var unitProduct = await _productRepository.Query()
                        .FirstOrDefaultAsync(p =>
                            p.CatalogProductId == componentCatalogId &&
                            p.TenantId == tenantId);

                    if (unitProduct == null)
                        throw new NotFoundException(
                            "Unit product for pack", componentCatalogId ?? Guid.Empty);

                    lineResolutions.Add(new PurchaseLineResolution
                    {
                        OriginalLine = line,
                        StockProductId = unitProduct.Id,
                        StockQuantity = unitQuantity,
                        IsPack = true,
                        PackSize = packSize
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

            // ── Calcul des totaux (sur les lignes originales — prix pack) ────────────
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

            // ── Création du Purchase ─────────────────────────────────────────────────
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

            // ── Purchase Lines (on garde le ProductId original du pack pour le doc) ──
            foreach (var line in request.Lines)
            {
                await _purchaseLineRepository.AddAsync(new PurchaseLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PurchaseId = purchase.Id,
                    ProductId = line.ProductId,      // ID du pack — pour le document
                    QuantityOrdered = line.Quantity,
                    QuantityReceived = line.Quantity,
                    UnitPurchasePrice = line.UnitPrice,
                    VatRate = line.VatRate
                });
            }

            // ── Stock : ajouter sur le produit UNITÉ ─────────────────────────────────
            // Grouper par StockProductId pour éviter les doublons si même unité
            var grouped = lineResolutions
                .GroupBy(r => r.StockProductId)
                .Select(g => new
                {
                    StockProductId = g.Key,
                    TotalQty = g.Sum(r => r.StockQuantity),
                    IsPack = g.First().IsPack,
                    PackSize = g.First().PackSize
                });

            foreach (var group in grouped)
            {
                var stock = await _stockRepository.GetSingleAsync(
                    s => s.ProductId == group.StockProductId &&
                         !s.IsDeleted &&
                         s.TenantId == tenantId);

                var quantityBefore = stock?.Quantity ?? 0;
                var quantityAfter = quantityBefore + group.TotalQty;

                var notes = group.IsPack
                    ? $"Purchase {purchase.PurchaseNumber} (pack x{group.PackSize})"
                    : $"Purchase {purchase.PurchaseNumber}";

                await _stockMovementRepository.AddAsync(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = group.StockProductId,
                    Type = StockMovementType.Purchase,
                    QuantityChange = group.TotalQty,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = quantityAfter,
                    ReferenceId = purchase.Id,
                    ReferenceNumber = purchase.PurchaseNumber,
                    MovementDate = DateTime.UtcNow,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

                if (stock == null)
                {
                    await _stockRepository.AddAsync(new Stock
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ProductId = group.StockProductId,
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

            // ── Paiement ─────────────────────────────────────────────────────────────
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

            // ── Cash Movement ────────────────────────────────────────────────────────
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
                    Reason = $"Purchase {purchase.PurchaseNumber}",
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
            var tenantId = _tenantContext?.TenantId;

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
                .Skip(query.Page - 1 * query.PageSize)
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
