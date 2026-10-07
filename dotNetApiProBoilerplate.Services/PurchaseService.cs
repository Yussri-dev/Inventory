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
        public async Task<PurchaseResult> CreateCompleteAsync(
     CreateCompletePurchaseRequest request)
        {
            if (request == null)
            {
                throw new ValidationException(
                    "Request cannot be null.");
            }

            var tenantId =
                _tenantContext.TenantId;

            if (request.ClientOperationId == Guid.Empty)
            {
                throw new ValidationException(
                    "ClientOperationId is required.");
            }

            if (request.SupplierId == Guid.Empty)
            {
                throw new ValidationException(
                    "SupplierId is required.");
            }

            if (request.Lines == null ||
                request.Lines.Count == 0)
            {
                throw new ValidationException(
                    "Purchase must contain at least one line.");
            }

            /*
             * Idempotency check must happen before:
             * - cash-session validation;
             * - stock updates;
             * - document-number generation;
             * - purchase creation.
             */
            var existingPurchase =
                await _repository.Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(purchase =>
                        purchase.TenantId == tenantId &&
                        purchase.ClientOperationId ==
                            request.ClientOperationId);

            if (existingPurchase != null)
            {
                return _mapper.Map<PurchaseResult>(
                    existingPurchase);
            }

            // ============================================================
            // VALIDATE PURCHASE LINES
            // ============================================================

            foreach (var line in request.Lines)
            {
                if (line.ProductId == Guid.Empty)
                {
                    throw new ValidationException(
                        "Every purchase line requires a ProductId.");
                }

                if (line.Quantity <= 0)
                {
                    throw new ValidationException(
                        "Purchase quantity must be greater than zero.");
                }

                if (line.UnitPrice < 0)
                {
                    throw new ValidationException(
                        "Unit purchase price cannot be negative.");
                }

                if (line.VatRate < 0 ||
                    line.VatRate > 100)
                {
                    throw new ValidationException(
                        "VAT rate must be between zero and 100.");
                }

                if (line.DiscountPercent < 0 ||
                    line.DiscountPercent > 100)
                {
                    throw new ValidationException(
                        "Discount percent must be between zero and 100.");
                }
            }

            // ============================================================
            // SUPPLIER
            // ============================================================

            var supplier =
                await _supplierRepository.GetSingleAsync(
                    supplier =>
                        supplier.Id == request.SupplierId &&
                        !supplier.IsDeleted &&
                        supplier.TenantId == tenantId);

            if (supplier == null)
            {
                throw new NotFoundException(
                    "Supplier",
                    request.SupplierId);
            }

            // ============================================================
            // LOAD PRODUCTS
            // ============================================================

            var productIds =
                request.Lines
                    .Select(line =>
                        line.ProductId)
                    .Distinct()
                    .ToList();

            var products =
                await _productRepository.Query()
                    .Include(product =>
                        product.CatalogProduct)
                    .Where(product =>
                        productIds.Contains(product.Id) &&
                        product.TenantId == tenantId &&
                        !product.IsDeleted)
                    .ToListAsync();

            var productMap =
                products.ToDictionary(
                    product =>
                        product.Id);

            var missingProductId =
                productIds.FirstOrDefault(
                    productId =>
                        !productMap.ContainsKey(
                            productId));

            if (missingProductId != Guid.Empty)
            {
                throw new NotFoundException(
                    "Product",
                    missingProductId);
            }

            // ============================================================
            // NORMALIZE PURCHASE LINES
            // ============================================================

            /*
             * Store the effective purchase price because PurchaseLine
             * currently has no DiscountPercent property.
             *
             * This keeps persisted lines consistent with header totals.
             */
            var normalizedLines =
                request.Lines
                    .Select(line =>
                    {
                        var effectiveUnitPrice =
                            Math.Round(
                                line.UnitPrice *
                                (1m -
                                 line.DiscountPercent / 100m),
                                2,
                                MidpointRounding.AwayFromZero);

                        var quantity =
                            Math.Round(
                                line.Quantity,
                                3,
                                MidpointRounding.AwayFromZero);

                        var normalizedVatRate =
                            Math.Round(
                                line.VatRate,
                                2,
                                MidpointRounding.AwayFromZero);

                        var lineAmountExclVat =
                            Math.Round(
                                quantity *
                                effectiveUnitPrice,
                                2,
                                MidpointRounding.AwayFromZero);

                        var lineVatAmount =
                            Math.Round(
                                lineAmountExclVat *
                                normalizedVatRate /
                                100m,
                                2,
                                MidpointRounding.AwayFromZero);

                        return new
                        {
                            Source =
                                line,

                            Quantity =
                                quantity,

                            EffectiveUnitPrice =
                                effectiveUnitPrice,

                            VatRate =
                                normalizedVatRate,

                            AmountExclVat =
                                lineAmountExclVat,

                            VatAmount =
                                lineVatAmount
                        };
                    })
                    .ToList();

            // ============================================================
            // CALCULATE TOTALS
            // ============================================================

            var totalExclVat =
                Math.Round(
                    normalizedLines.Sum(line =>
                        line.AmountExclVat),
                    2,
                    MidpointRounding.AwayFromZero);

            var totalVat =
                Math.Round(
                    normalizedLines.Sum(line =>
                        line.VatAmount),
                    2,
                    MidpointRounding.AwayFromZero);

            var totalInclVat =
                Math.Round(
                    totalExclVat +
                    totalVat,
                    2,
                    MidpointRounding.AwayFromZero);

            // ============================================================
            // PAYMENT VALIDATION
            // ============================================================

            PaymentMethod? paymentMethod =
                null;

            Guid? activeCashSessionId =
                null;

            if (request.Payment != null)
            {
                if (request.Payment.Amount <= 0)
                {
                    throw new ValidationException(
                        "Payment amount must be greater than zero.");
                }

                if (request.Payment.Amount > totalInclVat)
                {
                    throw new ValidationException(
                        "Payment amount cannot exceed the purchase total.");
                }

                if (!Enum.TryParse<PaymentMethod>(
                        request.Payment.PaymentMethod,
                        true,
                        out var parsedMethod))
                {
                    throw new ValidationException(
                        "Invalid payment method.");
                }

                paymentMethod =
                    parsedMethod;

                /*
                 * A cash session is required only for a cash payment.
                 * Unpaid, card or bank purchases do not need it.
                 */
                if (paymentMethod ==
                    PaymentMethod.Cash)
                {
                    activeCashSessionId =
                        await _cashSessionService
                            .EnsureActiveSessionAsync();
                }
            }

            // ============================================================
            // DATES
            // ============================================================

            var purchaseDate =
                NormalizeUtc(
                    request.PurchaseDate == default
                        ? DateTime.UtcNow
                        : request.PurchaseDate);

            var now =
                DateTime.UtcNow;

            // ============================================================
            // CREATE PURCHASE HEADER
            // ============================================================

            var purchase =
                new Purchase
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    ClientOperationId =
                        request.ClientOperationId,

                    SupplierId =
                        request.SupplierId,

                    PurchaseNumber =
                        await _documentNumberService
                            .GenerateAsync(
                                "PURCHASE"),

                    PurchaseDate =
                        purchaseDate,

                    DeliveryDate =
                        purchaseDate,

                    TotalAmountExclVat =
                        totalExclVat,

                    TotalVatAmount =
                        totalVat,

                    TotalAmountInclVat =
                        totalInclVat,

                    Status =
                        PurchaseStatus.Received,

                    PaymentDate =
                        request.Payment != null
                            ? now
                            : null,

                    CreatedAt =
                        now,

                    ModifiedAt =
                        now
                };

            await _repository.AddAsync(
                purchase);

            // ============================================================
            // CREATE PURCHASE LINES
            // ============================================================

            foreach (var normalizedLine in normalizedLines)
            {
                await _purchaseLineRepository.AddAsync(
                    new PurchaseLine
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        PurchaseId =
                            purchase.Id,

                        ProductId =
                            normalizedLine.Source.ProductId,

                        QuantityOrdered =
                            normalizedLine.Quantity,

                        QuantityReceived =
                            normalizedLine.Quantity,

                        /*
                         * Effective price is stored because PurchaseLine
                         * has no separate DiscountPercent property.
                         */
                        UnitPurchasePrice =
                            normalizedLine.EffectiveUnitPrice,

                        VatRate =
                            normalizedLine.VatRate
                    });
            }

            // ============================================================
            // RESOLVE STOCK PRODUCTS
            // ============================================================

            /*
             * Three possible cases:
             *
             * 1. Custom Product
             *    CatalogProductId == null
             *    => stock belongs directly to Product.
             *
             * 2. Normal catalog Product
             *    CatalogProductId != null
             *    IsPack == false
             *    => stock belongs directly to Product.
             *
             * 3. Catalog pack
             *    CatalogProductId != null
             *    IsPack == true
             *    => stock belongs to the component/unit Product.
             */

            var catalogIds =
                products
                    .Where(product =>
                        product.CatalogProductId.HasValue &&
                        product.CatalogProductId.Value != Guid.Empty)
                    .Select(product =>
                        product.CatalogProductId!.Value)
                    .Distinct()
                    .ToList();

            var componentCatalogIds =
                catalogIds
                    .Where(catalogId =>
                        _packService.IsPack(
                            catalogId))
                    .Select(catalogId =>
                        _packService.GetComponentCatalogId(
                            catalogId))
                    .Where(componentId =>
                        componentId.HasValue &&
                        componentId.Value != Guid.Empty)
                    .Select(componentId =>
                        componentId!.Value)
                    .Distinct()
                    .ToList();

            var allCatalogIds =
                catalogIds
                    .Concat(
                        componentCatalogIds)
                    .Distinct()
                    .ToList();

            List<Product> requiredProducts;

            if (allCatalogIds.Count == 0)
            {
                requiredProducts =
                    new List<Product>();
            }
            else
            {
                requiredProducts =
                    await _productRepository.Query()
                        .Where(product =>
                            product.CatalogProductId.HasValue &&
                            allCatalogIds.Contains(
                                product.CatalogProductId.Value) &&
                            product.TenantId == tenantId &&
                            !product.IsDeleted)
                        .ToListAsync();
            }

            var productsByCatalogId =
                requiredProducts
                    .Where(product =>
                        product.CatalogProductId.HasValue &&
                        product.CatalogProductId.Value != Guid.Empty)
                    .GroupBy(product =>
                        product.CatalogProductId!.Value)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.First());

            var stockResolutions =
                new List<PurchaseLineResolution>();

            foreach (var normalizedLine in normalizedLines)
            {
                var product =
                    productMap[
                        normalizedLine.Source.ProductId];

                // ========================================================
                // CASE 1: CUSTOM PRODUCT
                // ========================================================

                if (!product.CatalogProductId.HasValue ||
                    product.CatalogProductId.Value == Guid.Empty)
                {
                    stockResolutions.Add(
                        new PurchaseLineResolution
                        {
                            OriginalLine =
                                normalizedLine.Source,

                            StockProductId =
                                product.Id,

                            StockQuantity =
                                normalizedLine.Quantity,

                            IsPack =
                                false,

                            PackSize =
                                1m
                        });

                    continue;
                }

                var catalogId =
                    product.CatalogProductId.Value;

                // ========================================================
                // CASE 2: NORMAL CATALOG PRODUCT
                // ========================================================

                if (!_packService.IsPack(
                        catalogId))
                {
                    stockResolutions.Add(
                        new PurchaseLineResolution
                        {
                            OriginalLine =
                                normalizedLine.Source,

                            StockProductId =
                                product.Id,

                            StockQuantity =
                                normalizedLine.Quantity,

                            IsPack =
                                false,

                            PackSize =
                                1m
                        });

                    continue;
                }

                // ========================================================
                // CASE 3: PACK
                // ========================================================

                var componentCatalogId =
                    _packService
                        .GetComponentCatalogId(
                            catalogId);

                if (!componentCatalogId.HasValue ||
                    componentCatalogId.Value == Guid.Empty)
                {
                    throw new ValidationException(
                        $"Pack configuration is invalid for " +
                        $"Product '{product.Id}'.");
                }

                if (!productsByCatalogId.TryGetValue(
                        componentCatalogId.Value,
                        out var unitProduct))
                {
                    throw new NotFoundException(
                        "Unit product",
                        componentCatalogId.Value);
                }

                var packSize =
                    _packService.GetPackSize(
                        catalogId);

                if (packSize <= 0)
                {
                    throw new ValidationException(
                        $"Pack size is invalid for Product " +
                        $"'{product.Id}'.");
                }

                var unitQuantity =
                    _packService.GetUnitQuantity(
                        catalogId,
                        normalizedLine.Quantity);

                if (unitQuantity <= 0)
                {
                    throw new ValidationException(
                        $"Calculated unit quantity is invalid for " +
                        $"Product '{product.Id}'.");
                }

                stockResolutions.Add(
                    new PurchaseLineResolution
                    {
                        OriginalLine =
                            normalizedLine.Source,

                        StockProductId =
                            unitProduct.Id,

                        StockQuantity =
                            unitQuantity,

                        IsPack =
                            true,

                        PackSize =
                            packSize
                    });
            }

            // ============================================================
            // UPDATE STOCK + CREATE STOCK MOVEMENTS
            // ============================================================

            foreach (var group in stockResolutions
                         .GroupBy(resolution =>
                             resolution.StockProductId))
            {
                var stock =
                    await _stockRepository.GetSingleAsync(
                        item =>
                            item.ProductId == group.Key &&
                            !item.IsDeleted &&
                            item.TenantId == tenantId);

                var quantityBefore =
                    stock?.Quantity ?? 0m;

                var quantityChange =
                    Math.Round(
                        group.Sum(item =>
                            item.StockQuantity),
                        3,
                        MidpointRounding.AwayFromZero);

                var quantityAfter =
                    quantityBefore +
                    quantityChange;

                await _stockMovementRepository.AddAsync(
                    new StockMovement
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        ProductId =
                            group.Key,

                        Type =
                            StockMovementType.Purchase,

                        QuantityChange =
                            quantityChange,

                        QuantityBefore =
                            quantityBefore,

                        QuantityAfter =
                            quantityAfter,

                        ReferenceId =
                            purchase.Id,

                        ReferenceNumber =
                            purchase.PurchaseNumber,

                        MovementDate =
                            now,

                        CreatedAt =
                            now,

                        ModifiedAt =
                            now
                    });

                if (stock == null)
                {
                    await _stockRepository.AddAsync(
                        new Stock
                        {
                            Id =
                                Guid.NewGuid(),

                            TenantId =
                                tenantId,

                            ProductId =
                                group.Key,

                            Quantity =
                                quantityAfter,

                            CreatedAt =
                                now,

                            ModifiedAt =
                                now
                        });
                }
                else
                {
                    stock.Quantity =
                        quantityAfter;

                    stock.ModifiedAt =
                        now;

                    _stockRepository.Update(
                        stock);
                }
            }

            // ============================================================
            // PURCHASE PAYMENT
            // ============================================================

            if (request.Payment != null &&
                paymentMethod.HasValue)
            {
                await _paymentRepository.AddAsync(
                    new PurchasePayment
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        PurchaseId =
                            purchase.Id,

                        Method =
                            paymentMethod.Value,

                        Amount =
                            Math.Round(
                                request.Payment.Amount,
                                2,
                                MidpointRounding.AwayFromZero),

                        TransactionRef =
                            request.Payment.Reference,

                        PaymentDate =
                            now,

                        CreatedAt =
                            now
                    });
            }

            // ============================================================
            // CASH MOVEMENT
            // ============================================================

            if (request.Payment != null &&
                paymentMethod == PaymentMethod.Cash &&
                activeCashSessionId.HasValue)
            {
                var lastMovement =
                    await _cashMovementRepository.GetLastAsync(
                        movement =>
                            movement.CashSessionId ==
                                activeCashSessionId.Value &&
                            !movement.IsDeleted &&
                            movement.TenantId == tenantId,
                        movement =>
                            movement.MovementDate);

                var balanceBefore =
                    lastMovement?.BalanceAfter ??
                    0m;

                var cashAmount =
                    Math.Round(
                        request.Payment.Amount,
                        2,
                        MidpointRounding.AwayFromZero);

                var balanceAfter =
                    balanceBefore -
                    cashAmount;

                if (balanceAfter < 0)
                {
                    throw new ValidationException(
                        "Cash drawer cannot go negative.");
                }

                await _cashMovementRepository.AddAsync(
                    new CashMovement
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        CashSessionId =
                            activeCashSessionId.Value,

                        Type =
                            CashMovementType.Withdrawal,

                        Amount =
                            cashAmount,

                        BalanceBefore =
                            balanceBefore,

                        BalanceAfter =
                            balanceAfter,

                        ReferenceId =
                            purchase.Id,

                        MovementDate =
                            now,

                        CreatedAt =
                            now
                    });
            }

            // ============================================================
            // SUPPLIER BALANCE
            // ============================================================

            var paidAmount =
                Math.Round(
                    request.Payment?.Amount ?? 0m,
                    2,
                    MidpointRounding.AwayFromZero);

            supplier.CurrentBalance +=
                totalInclVat -
                paidAmount;

            supplier.ModifiedAt =
                now;

            _supplierRepository.Update(
                supplier);

            // ============================================================
            // SUPPLIER TRANSACTION - PURCHASE
            // ============================================================

            await _supplierTransactionRepository.AddAsync(
                new SupplierTransaction
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    SupplierId =
                        supplier.Id,

                    PurchaseId =
                        purchase.Id,

                    Type =
                        SupplierTransactionType.Purchase,

                    Amount =
                        totalInclVat,

                    TransactionDate =
                        purchaseDate,

                    ReferenceNumber =
                        purchase.PurchaseNumber,

                    CreatedAt =
                        now,

                    ModifiedAt =
                        now
                });

            // ============================================================
            // SUPPLIER TRANSACTION - PAYMENT
            // ============================================================

            if (paidAmount > 0)
            {
                await _supplierTransactionRepository.AddAsync(
                    new SupplierTransaction
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        SupplierId =
                            supplier.Id,

                        PurchaseId =
                            purchase.Id,

                        Type =
                            SupplierTransactionType.Payment,

                        Amount =
                            paidAmount,

                        TransactionDate =
                            now,

                        ReferenceNumber =
                            purchase.PurchaseNumber,

                        CreatedAt =
                            now,

                        ModifiedAt =
                            now
                    });
            }

            // ============================================================
            // SAVE TRANSACTION
            // ============================================================

            try
            {
                /*
                 * One SaveChanges means EF Core wraps all inserted
                 * and updated rows in one database transaction.
                 */
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                /*
                 * Final protection against two concurrent requests
                 * using the same ClientOperationId.
                 */
                var concurrentExistingPurchase =
                    await _repository.Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item =>
                            item.TenantId == tenantId &&
                            item.ClientOperationId ==
                                request.ClientOperationId);

                if (concurrentExistingPurchase != null)
                {
                    return _mapper.Map<PurchaseResult>(
                        concurrentExistingPurchase);
                }

                throw;
            }

            return _mapper.Map<PurchaseResult>(
                purchase);
        }

        private static DateTime NormalizeUtc(
    DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc =>
                    value,

                DateTimeKind.Local =>
                    value.ToUniversalTime(),

                _ =>
                    DateTime.SpecifyKind(
                        value,
                        DateTimeKind.Utc)
            };
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
