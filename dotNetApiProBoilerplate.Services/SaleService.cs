using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.SaleLines.Requests;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class SaleService
    {
        private readonly IRepository<Sale> _repository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductCatalog> _productCatalogRepository;
        private readonly IRepository<SaleLine> _saleLineRepository;
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<CustomerTransaction> _customerTransactionRepository;
        private readonly IRepository<CashMovement> _cashMovementRepository;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<CashSession> _cashSessionRepository;
        private readonly IRepository<LoyaltyCard> _loyaltyCardRepository;
        private readonly IRepository<LoyaltyTransaction> _loyaltyTransactionRepository;
        private readonly IRepository<SalesSummaryDaily> _salesSummaryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDocumentNumberService _documentNumberService;
        private readonly ICashSessionService _cashSessionService;
        private readonly ITenantContext _tenantContext;
        private readonly IPackService _packService;
        public SaleService(
            IRepository<Sale> repository,
            IRepository<SaleLine> saleLineRepository,
            IRepository<StockMovement> stockMovementRepository,
            IRepository<Stock> stockRepository,
            IRepository<Payment> paymentRepository,
            IRepository<Customer> customerRepository,
            IRepository<CashSession> cashSessionRepository,
            IRepository<CustomerTransaction> customerTransactionRepository,
            IRepository<CashMovement> cashMovementRepository,
            IRepository<LoyaltyCard> loyaltyCardRepository,
            IRepository<LoyaltyTransaction> loyaltyTransactionRepository,
            IRepository<SalesSummaryDaily> salesSummaryRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICashSessionService cashSessionService,
            IDocumentNumberService documentNumberService,
            IRepository<Tenant> tenantRepository,
            IPackService packService,
            IRepository<Product> productRepository,
            IRepository<ProductCatalog> productCatalogRepository,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _saleLineRepository = saleLineRepository;
            _stockMovementRepository = stockMovementRepository;
            _stockRepository = stockRepository;
            _paymentRepository = paymentRepository;
            _customerRepository = customerRepository;
            _customerTransactionRepository = customerTransactionRepository;
            _cashMovementRepository = cashMovementRepository;
            _loyaltyCardRepository = loyaltyCardRepository;
            _loyaltyTransactionRepository = loyaltyTransactionRepository;
            _salesSummaryRepository = salesSummaryRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _documentNumberService = documentNumberService;
            _tenantContext = tenantContext;
            _tenantRepository = tenantRepository;
            _cashSessionService = cashSessionService;
            _cashSessionRepository = cashSessionRepository;
            _packService = packService;
            _productRepository = productRepository;
            _productCatalogRepository = productCatalogRepository;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<SaleResult> CreateAsync(CreateSaleRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            if (request.TotalAmount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmount", new[] { "TotalAmount must be greater than 0." } }
                });
            }

            var sale = _mapper.Map<Sale>(request);

            sale.Id = Guid.NewGuid();
            sale.TenantId = tenantId;
            sale.InvoiceNumber = await _documentNumberService.GenerateAsync("191125");
            sale.SaleDate = EnsureUtc(request.SaleDate == default ? DateTime.UtcNow : request.SaleDate);
            sale.TotalAmount = request.TotalAmount;
            sale.CreatedAt = DateTime.UtcNow;
            sale.ModifiedAt = DateTime.UtcNow;

            sale.PaymentStatus = PaymentStatus.Paid;

            await _repository.AddAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(sale);
        }


        private static DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value;

            if (value.Kind == DateTimeKind.Local)
                return value.ToUniversalTime();

            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public async Task<SaleResult> CreateCompleteAsync(
    CreateCompleteSaleRequest request)
        {
            if (!_tenantContext.IsCashier &&
                !_tenantContext.IsAdmin)
            {
                throw new ForbiddenException(
                    "Only cashiers or admins can create sales.");
            }

            if (request == null)
            {
                throw new ValidationException(
                    "Request cannot be null.");
            }

            var tenantId =
                _tenantContext.TenantId;

            // ============================================================
            // IDEMPOTENCY
            // ============================================================

            if (request.ClientOperationId == Guid.Empty)
            {
                throw new ValidationException(
                    "ClientOperationId is required.");
            }

            var existingOperationSale =
                await _repository.Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        sale =>
                            sale.TenantId == tenantId &&
                            sale.ClientOperationId ==
                                request.ClientOperationId &&
                            !sale.IsDeleted);

            if (existingOperationSale != null &&
                existingOperationSale.Status ==
                    SaleStatus.Completed)
            {
                return _mapper.Map<SaleResult>(
                    existingOperationSale);
            }

            // ============================================================
            // CASH SESSION
            // ============================================================

            Guid cashSessionId;

            if (request.CashSessionId.HasValue &&
                request.CashSessionId.Value != Guid.Empty)
            {
                /*
                 * Synchronisation d'une vente locale :
                 * conserver la session de caisse d'origine.
                 */
                cashSessionId =
                    request.CashSessionId.Value;
            }
            else
            {
                /*
                 * Vente créée directement en ligne :
                 * utiliser la session active actuelle.
                 */
                cashSessionId =
                    await _cashSessionService
                        .EnsureActiveSessionAsync();
            }

            var cashSession =
                await _cashSessionRepository
                    .GetSingleAsync(session =>
                        session.Id == cashSessionId &&
                        session.TenantId == tenantId &&
                        !session.IsDeleted);

            if (cashSession == null)
            {
                throw new ValidationException(
                    $"Cash session '{cashSessionId}' was not found " +
                    "for the current tenant.");
            }

            // ============================================================
            // LINES VALIDATION
            // ============================================================

            if (request.Lines == null ||
                request.Lines.Count == 0)
            {
                throw new ValidationException(
                    "Sale must contain at least one line.");
            }

            foreach (var line in request.Lines)
            {
                if (line.ProductId == Guid.Empty)
                {
                    throw new ValidationException(
                        "Every sale line requires a ProductId.");
                }

                if (line.Quantity <= 0)
                {
                    throw new ValidationException(
                        $"Quantity must be greater than 0 " +
                        $"for product {line.ProductId}.");
                }

                if (line.UnitPrice < 0)
                {
                    throw new ValidationException(
                        $"UnitPrice cannot be negative " +
                        $"for product {line.ProductId}.");
                }

                if (line.DiscountPercent < 0 ||
                    line.DiscountPercent > 100)
                {
                    throw new ValidationException(
                        $"DiscountPercent must be between 0 and 100 " +
                        $"for product {line.ProductId}.");
                }

                if (line.VatRate < 0 ||
                    line.VatRate > 100)
                {
                    throw new ValidationException(
                        $"VatRate must be between 0 and 100 " +
                        $"for product {line.ProductId}.");
                }
            }

            // ============================================================
            // CREATE OR LOAD SALE
            // ============================================================

            Sale sale;

            var effectivePendingSaleId =
                request.PendingSaleId ??
                existingOperationSale?.Id;

            var isExistingPending =
                effectivePendingSaleId.HasValue;

            if (isExistingPending)
            {
                sale =
                    await _repository.Query()
                        .Include(sale =>
                            sale.Lines)
                        .FirstOrDefaultAsync(
                            sale =>
                                sale.Id ==
                                    effectivePendingSaleId!.Value &&
                                sale.Status ==
                                    SaleStatus.Pending &&
                                !sale.IsDeleted &&
                                sale.TenantId ==
                                    tenantId)
                    ?? throw new NotFoundException(
                        "Pending Sale",
                        effectivePendingSaleId!.Value);

                if (sale.ClientOperationId ==
                    Guid.Empty)
                {
                    sale.ClientOperationId =
                        request.ClientOperationId;
                }
                else if (
                    sale.ClientOperationId !=
                    request.ClientOperationId)
                {
                    throw new ValidationException(
                        "The pending sale belongs to another operation.");
                }

                var existingLines =
                    await _saleLineRepository
                        .GetAsync(
                            line =>
                                line.SaleId ==
                                    sale.Id);

                foreach (var line in existingLines)
                {
                    _saleLineRepository.Delete(
                        line);
                }
            }
            else
            {
                sale =
                    new Sale
                    {
                        Id =
                            Guid.NewGuid(),

                        ClientOperationId =
                            request.ClientOperationId,

                        TenantId =
                            tenantId,

                        InvoiceNumber =
                            await _documentNumberService
                                .GenerateAsync(
                                    "191125"),

                        CashSessionId =
                            cashSessionId,

                        SaleDate =
                            request.SaleDate == default
                                ? DateTime.UtcNow
                                : EnsureUtc(
                                    request.SaleDate),

                        CreatedAt =
                            DateTime.UtcNow,

                        ModifiedAt =
                            DateTime.UtcNow,

                        Status =
                            SaleStatus.Pending,

                        PaymentStatus =
                            PaymentStatus.Pending
                    };

                await _repository.AddAsync(
                    sale);

                await _unitOfWork
                    .SaveChangesAsync();
            }

            // ============================================================
            // LOAD PRODUCTS
            // ============================================================

            var requestProductIds =
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
                        requestProductIds.Contains(
                            product.Id) &&
                        product.TenantId ==
                            tenantId &&
                        !product.IsDeleted)
                    .ToListAsync();

            var productMap =
                products.ToDictionary(
                    product =>
                        product.Id);

            var missingProductId =
                requestProductIds.FirstOrDefault(
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
            // LOAD CATALOG PACK INFORMATION
            // ============================================================

            /*
             * Important:
             * custom products have CatalogProductId = null.
             *
             * They must NEVER be passed to PackService.
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

            List<ProductCatalog> packCatalogs;

            if (catalogIds.Count == 0)
            {
                packCatalogs =
                    new List<ProductCatalog>();
            }
            else
            {
                packCatalogs =
                    await _productCatalogRepository.Query()
                        .Include(catalog =>
                            catalog.PackComponents)
                        .Where(catalog =>
                            catalogIds.Contains(
                                catalog.Id) &&
                            catalog.IsPack &&
                            !catalog.IsDeleted)
                        .ToListAsync();
            }

            var componentCatalogIds =
                packCatalogs
                    .SelectMany(catalog =>
                        catalog.PackComponents)
                    .Select(component =>
                        component.ComponentCatalogId)
                    .Where(id =>
                        id != Guid.Empty)
                    .Distinct()
                    .ToList();

            List<Product> allTenantProductsForCatalogs;

            if (componentCatalogIds.Count == 0)
            {
                allTenantProductsForCatalogs =
                    new List<Product>();
            }
            else
            {
                allTenantProductsForCatalogs =
                    await _productRepository.Query()
                        .Include(product =>
                            product.CatalogProduct)
                        .Where(product =>
                            product.CatalogProductId.HasValue &&
                            componentCatalogIds.Contains(
                                product.CatalogProductId.Value) &&
                            product.TenantId ==
                                tenantId &&
                            !product.IsDeleted)
                        .ToListAsync();
            }

            var unitProductMap =
                allTenantProductsForCatalogs
                    .Where(product =>
                        product.CatalogProductId.HasValue &&
                        product.CatalogProductId.Value != Guid.Empty &&
                        product.CatalogProduct != null &&
                        !product.CatalogProduct.IsPack)
                    .GroupBy(product =>
                        product.CatalogProductId!.Value)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.First());

            // ============================================================
            // RESOLVE SALE LINES
            // ============================================================

            /*
             * Three cases:
             *
             * 1. Custom Product
             *    CatalogProductId == null
             *    => stock is this Product itself.
             *
             * 2. Normal catalog Product
             *    => stock is this Product itself.
             *
             * 3. Pack catalog Product
             *    => stock is the component Product.
             */
            var lineResolutions =
                new List<LineResolution>();

            foreach (var line in request.Lines)
            {
                var product =
                    productMap[line.ProductId];

                // ========================================================
                // CUSTOM PRODUCT
                // ========================================================

                if (!product.CatalogProductId.HasValue ||
                    product.CatalogProductId.Value == Guid.Empty)
                {
                    lineResolutions.Add(
                        new LineResolution
                        {
                            OriginalLine =
                                line,

                            StockProductId =
                                product.Id,

                            StockQuantity =
                                line.Quantity,

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
                // NORMAL CATALOG PRODUCT
                // ========================================================

                if (!_packService.IsPack(
                        catalogId))
                {
                    lineResolutions.Add(
                        new LineResolution
                        {
                            OriginalLine =
                                line,

                            StockProductId =
                                product.Id,

                            StockQuantity =
                                line.Quantity,

                            IsPack =
                                false,

                            PackSize =
                                1m
                        });

                    continue;
                }

                // ========================================================
                // PACK PRODUCT
                // ========================================================

                var componentCatalogId =
                    _packService
                        .GetComponentCatalogId(
                            catalogId);

                if (!componentCatalogId.HasValue ||
                    componentCatalogId.Value ==
                        Guid.Empty)
                {
                    throw new ValidationException(
                        $"Pack configuration is invalid " +
                        $"for catalog '{catalogId}'.");
                }

                if (!unitProductMap.TryGetValue(
                        componentCatalogId.Value,
                        out var unitProduct))
                {
                    throw new ValidationException(
                        $"Unit product not found for catalog " +
                        $"{componentCatalogId.Value}. " +
                        "Pack configuration is broken.");
                }

                var packSize =
                    _packService.GetPackSize(
                        catalogId);

                if (packSize <= 0)
                {
                    throw new ValidationException(
                        $"Pack size is invalid for catalog " +
                        $"'{catalogId}'.");
                }

                var unitQuantity =
                    _packService.GetUnitQuantity(
                        catalogId,
                        line.Quantity);

                if (unitQuantity <= 0)
                {
                    throw new ValidationException(
                        $"Calculated unit quantity is invalid " +
                        $"for product '{product.Id}'.");
                }

                lineResolutions.Add(
                    new LineResolution
                    {
                        OriginalLine =
                            line,

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
            // VALIDATE STOCK
            // ============================================================

            var resolvedProductIds =
                lineResolutions
                    .Select(resolution =>
                        resolution.StockProductId)
                    .Distinct()
                    .ToList();

            var stocks =
                await _stockRepository.GetAsync(
                    stock =>
                        resolvedProductIds.Contains(
                            stock.ProductId) &&
                        !stock.IsDeleted &&
                        stock.TenantId ==
                            tenantId);

            var stockMap =
                stocks.ToDictionary(
                    stock =>
                        stock.ProductId);

            var requiredByProduct =
                lineResolutions
                    .GroupBy(resolution =>
                        resolution.StockProductId)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.Sum(
                                resolution =>
                                    resolution.StockQuantity));

            foreach (var requirement in requiredByProduct)
            {
                if (!stockMap.TryGetValue(
                        requirement.Key,
                        out var stock))
                {
                    throw new NotFoundException(
                        "Stock",
                        requirement.Key);
                }

                if (stock.Quantity <
                    requirement.Value)
                {
                    throw new ValidationException(
                        $"Insufficient stock for product " +
                        $"{requirement.Key}. " +
                        $"Required: {requirement.Value}, " +
                        $"Available: {stock.Quantity}");
                }
            }

            // ============================================================
            // DEDUCT STOCK
            // ============================================================

            var stockMovements =
                new List<StockMovement>();

            var now =
                DateTime.UtcNow;

            foreach (var requirement in requiredByProduct)
            {
                var stock =
                    stockMap[
                        requirement.Key];

                var quantityBefore =
                    stock.Quantity;

                var quantityAfter =
                    quantityBefore -
                    requirement.Value;

                stockMovements.Add(
                    new StockMovement
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        ProductId =
                            requirement.Key,

                        Type =
                            StockMovementType.Sale,

                        QuantityChange =
                            -requirement.Value,

                        QuantityBefore =
                            quantityBefore,

                        QuantityAfter =
                            quantityAfter,

                        ReferenceId =
                            sale.Id,

                        ReferenceNumber =
                            sale.InvoiceNumber,

                        MovementDate =
                            now,

                        Notes =
                            $"Sale {sale.InvoiceNumber}",

                        CreatedAt =
                            now,

                        ModifiedAt =
                            now
                    });

                stock.Quantity =
                    quantityAfter;

                stock.LastUpdated =
                    now;

                stock.ModifiedAt =
                    now;

                _stockRepository.Update(
                    stock);
            }

            await _stockMovementRepository
                .AddRangeAsync(
                    stockMovements);

            // ============================================================
            // CALCULATE TOTALS
            // ============================================================

            decimal subtotalAmount = 0m;
            decimal vatAmount = 0m;
            decimal totalAmount = 0m;

            foreach (var resolution in lineResolutions)
            {
                var line =
                    resolution.OriginalLine;

                var lineGross =
                    line.Quantity *
                    line.UnitPrice;

                var lineDiscount =
                    lineGross *
                    (line.DiscountPercent / 100m);

                var lineNetTtc =
                    lineGross -
                    lineDiscount;

                var divisor =
                    1m +
                    (line.VatRate / 100m);

                var lineHt =
                    divisor <= 0
                        ? lineNetTtc
                        : lineNetTtc /
                          divisor;

                var lineVat =
                    lineNetTtc -
                    lineHt;

                subtotalAmount +=
                    Math.Round(
                        lineHt,
                        2,
                        MidpointRounding.AwayFromZero);

                vatAmount +=
                    Math.Round(
                        lineVat,
                        2,
                        MidpointRounding.AwayFromZero);

                totalAmount +=
                    Math.Round(
                        lineNetTtc,
                        2,
                        MidpointRounding.AwayFromZero);
            }

            subtotalAmount =
                Math.Round(
                    subtotalAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            vatAmount =
                Math.Round(
                    vatAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            totalAmount =
                Math.Round(
                    totalAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            // ============================================================
            // VALIDATE PAYMENTS
            // ============================================================

            var paidAmount =
                Math.Round(
                    request.Payments?
                        .Sum(payment =>
                            payment.Amount)
                    ?? 0m,
                    2,
                    MidpointRounding.AwayFromZero);

            if (paidAmount < 0)
            {
                throw new ValidationException(
                    "Paid amount cannot be negative.");
            }

            if (request.ChangeAmount < 0)
            {
                throw new ValidationException(
                    "Change amount cannot be negative.");
            }

            if (request.ChangeAmount >
                paidAmount)
            {
                throw new ValidationException(
                    "Change amount cannot exceed paid amount.");
            }

            if ((paidAmount -
                 request.ChangeAmount) >
                totalAmount + 0.01m)
            {
                throw new ValidationException(
                    "Invalid payment / change combination.");
            }

            var netTendered =
                Math.Round(
                    paidAmount -
                    request.ChangeAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            if (netTendered + 0.01m <
                totalAmount)
            {
                throw new ValidationException(
                    "The payment amount is lower than the sale total.");
            }

            // ============================================================
            // PAYMENTS
            // ============================================================

            decimal cashAmount =
                0m;

            decimal cashAndCardPaid =
                0m;

            decimal creditAmount =
                0m;

            if (request.Payments != null &&
                request.Payments.Count > 0)
            {
                foreach (var paymentInfo
                         in request.Payments)
                {
                    if (!Enum.TryParse<PaymentMethod>(
                            paymentInfo.PaymentMethod,
                            true,
                            out var method))
                    {
                        throw new ValidationException(
                            $"Invalid payment method: " +
                            $"{paymentInfo.PaymentMethod}");
                    }

                    if (paymentInfo.Amount <= 0)
                    {
                        throw new ValidationException(
                            "Payment amount must be greater than 0.");
                    }

                    await _paymentRepository.AddAsync(
                        new Payment
                        {
                            Id =
                                Guid.NewGuid(),

                            SaleId =
                                sale.Id,

                            TenantId =
                                tenantId,

                            Method =
                                method,

                            Amount =
                                paymentInfo.Amount,

                            TransactionRef =
                                paymentInfo.Reference,

                            PaidAt =
                                now,

                            CreatedAt =
                                now,

                            ModifiedAt =
                                now
                        });

                    if (method ==
                        PaymentMethod.Credit)
                    {
                        creditAmount +=
                            paymentInfo.Amount;
                    }
                    else
                    {
                        cashAndCardPaid +=
                            paymentInfo.Amount;
                    }

                    if (method ==
                        PaymentMethod.Cash)
                    {
                        cashAmount +=
                            paymentInfo.Amount;
                    }
                }
            }

            // ============================================================
            // PAYMENT STATUS
            // ============================================================

            var realPaid =
                Math.Round(
                    Math.Max(
                        0m,
                        cashAndCardPaid -
                        request.ChangeAmount),
                    2,
                    MidpointRounding.AwayFromZero);

            var roundedTotal =
                Math.Round(
                    totalAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            var paymentStatus =
                creditAmount > 0m &&
                realPaid > 0m
                    ? PaymentStatus.PartiallyPaid
                    : creditAmount > 0m
                        ? PaymentStatus.Pending
                        : realPaid >= roundedTotal
                            ? PaymentStatus.Paid
                            : realPaid > 0m
                                ? PaymentStatus.PartiallyPaid
                                : PaymentStatus.Pending;

            // ============================================================
            // UPDATE SALE
            // ============================================================

            sale.CustomerId =
                request.CustomerId;

            sale.CashSessionId =
                cashSessionId;

            sale.SaleDate =
                request.SaleDate == default
                    ? now
                    : EnsureUtc(
                        request.SaleDate);

            sale.SubtotalAmount =
                subtotalAmount;

            sale.VatAmount =
                vatAmount;

            sale.TotalAmount =
                totalAmount;

            sale.PaidAmount =
                paidAmount;

            sale.ChangeAmount =
                request.ChangeAmount;

            sale.Status =
                SaleStatus.Completed;

            sale.PaymentStatus =
                paymentStatus;

            sale.Notes =
                request.Notes;

            sale.ModifiedAt =
                now;

            _repository.Update(
                sale);

            // ============================================================
            // SALE LINES
            // ============================================================

            /*
             * Build a lookup containing:
             * - products sold directly;
             * - possible component/unit products.
             */
            var resolvedProducts =
                products
                    .Concat(
                        allTenantProductsForCatalogs)
                    .GroupBy(product =>
                        product.Id)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.First());

            var saleLines =
                lineResolutions
                    .Select(resolution =>
                    {
                        var line =
                            resolution.OriginalLine;

                        if (resolution.StockQuantity <= 0)
                        {
                            throw new ValidationException(
                                $"Invalid stock quantity for product " +
                                $"{resolution.StockProductId}");
                        }

                        if (!resolvedProducts.TryGetValue(
                                resolution.StockProductId,
                                out var stockProduct))
                        {
                            throw new NotFoundException(
                                "Product",
                                resolution.StockProductId);
                        }

                        decimal unitCostPrice;

                        if (resolution.IsPack)
                        {
                            /*
                             * PurchasePrice on the component Product
                             * already represents one unit.
                             *
                             * Do NOT divide it by PackSize again.
                             */
                            unitCostPrice =
                                stockProduct.PurchasePrice;
                        }
                        else
                        {
                            unitCostPrice =
                                stockProduct.PurchasePrice;
                        }

                        var lineGross =
                            line.Quantity *
                            line.UnitPrice;

                        var lineDiscount =
                            lineGross *
                            (line.DiscountPercent /
                             100m);

                        return new SaleLine
                        {
                            Id =
                                Guid.NewGuid(),

                            TenantId =
                                tenantId,

                            SaleId =
                                sale.Id,

                            /*
                             * ProductId = product actually sold.
                             *
                             * For a pack, this remains the pack Product.
                             */
                            ProductId =
                                line.ProductId,

                            Quantity =
                                line.Quantity,

                            /*
                             * UnitProductId = Product whose stock changed.
                             *
                             * For a custom/normal product this is the
                             * same ProductId.
                             *
                             * For a pack this is the component Product.
                             */
                            UnitProductId =
                                resolution.StockProductId,

                            UnitQuantity =
                                resolution.StockQuantity,

                            UnitPrice =
                                line.UnitPrice,

                            VatRate =
                                line.VatRate,

                            DiscountPercent =
                                line.DiscountPercent,

                            DiscountAmount =
                                Math.Round(
                                    lineDiscount,
                                    2,
                                    MidpointRounding.AwayFromZero),

                            UnitCostPrice =
                                Math.Round(
                                    unitCostPrice,
                                    4,
                                    MidpointRounding.AwayFromZero),

                            CreatedAt =
                                now,

                            ModifiedAt =
                                now
                        };
                    })
                    .ToList();

            await _saleLineRepository
                .AddRangeAsync(
                    saleLines);

            // ============================================================
            // CASH DRAWER MOVEMENT
            // ============================================================

            if (cashAmount > 0)
            {
                var last =
                    await _cashMovementRepository
                        .GetLastAsync(
                            movement =>
                                movement.CashSessionId ==
                                    cashSessionId &&
                                !movement.IsDeleted &&
                                movement.TenantId ==
                                    tenantId,
                            movement =>
                                movement.MovementDate);

                var before =
                    last?.BalanceAfter ??
                    0m;

                var cashNetAmount =
                    Math.Round(
                        cashAmount -
                        request.ChangeAmount,
                        2,
                        MidpointRounding.AwayFromZero);

                var after =
                    before +
                    cashNetAmount;

                if (after < 0)
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
                            cashSessionId,

                        Type =
                            CashMovementType.Sale,

                        Amount =
                            cashNetAmount,

                        BalanceBefore =
                            before,

                        BalanceAfter =
                            after,

                        ReferenceId =
                            sale.Id,

                        ReferenceType =
                            "Sale",

                        MovementDate =
                            now,

                        CreatedAt =
                            now
                    });
            }

            // ============================================================
            // CUSTOMER CREDIT
            // ============================================================

            if (creditAmount >= 0.01m)
            {
                if (!sale.CustomerId.HasValue ||
                    sale.CustomerId.Value ==
                        Guid.Empty)
                {
                    throw new ValidationException(
                        "A customer is required for a credit sale.");
                }

                var customer =
                    await _customerRepository
                        .GetByIdAsync(
                            sale.CustomerId.Value);

                if (customer == null ||
                    customer.IsDeleted ||
                    customer.TenantId !=
                        tenantId)
                {
                    throw new NotFoundException(
                        "Customer",
                        sale.CustomerId.Value);
                }

                if (!customer.AllowCredit)
                {
                    throw new ValidationException(
                        "Credit is not enabled for this customer.");
                }

                var balanceBefore =
                    Math.Round(
                        customer.CurrentBalance,
                        2,
                        MidpointRounding.AwayFromZero);

                var balanceAfter =
                    Math.Round(
                        balanceBefore +
                        creditAmount,
                        2,
                        MidpointRounding.AwayFromZero);

                if (!customer.HasUnlimitedCredit &&
                    balanceAfter >
                        customer.CreditLimit)
                {
                    throw new ValidationException(
                        $"The customer credit limit would be exceeded. " +
                        $"Limit: {customer.CreditLimit:0.00}, " +
                        $"new balance: {balanceAfter:0.00}.");
                }

                customer.CurrentBalance =
                    balanceAfter;

                customer.ModifiedAt =
                    now;

                _customerRepository.Update(
                    customer);

                await _customerTransactionRepository
                    .AddAsync(
                        new CustomerTransaction
                        {
                            Id =
                                Guid.NewGuid(),

                            TenantId =
                                tenantId,

                            CustomerId =
                                customer.Id,

                            SaleId =
                                sale.Id,

                            Type =
                                "Credit",

                            Amount =
                                creditAmount,

                            BalanceBefore =
                                balanceBefore,

                            BalanceAfter =
                                balanceAfter,

                            TransactionDate =
                                now,

                            Description =
                                $"Credit sale {sale.InvoiceNumber}",

                            CreatedAt =
                                now,

                            ModifiedAt =
                                now
                        });
            }

            // ============================================================
            // SAVE
            // ============================================================

            await _unitOfWork
                .SaveChangesAsync();

            return _mapper.Map<SaleResult>(
                sale);
        }

        public async Task<SaleResult> UpdateCompletedAsync(
    Guid id,
    UpdateSaleRequest request)
        {
            if (request == null)
            {
                throw new ValidationException(
                    "Request cannot be null.");
            }

            if (id == Guid.Empty)
            {
                throw new ValidationException(
                    "Sale ID is required.");
            }

            var tenantId =
                _tenantContext.TenantId;

            // ============================================================
            // LOAD SALE
            // ============================================================

            var sale =
                await _repository.Query()
                    .Include(s =>
                        s.Lines)
                    .FirstOrDefaultAsync(s =>
                        s.Id == id &&
                        !s.IsDeleted &&
                        s.TenantId == tenantId);

            if (sale == null)
            {
                throw new NotFoundException(
                    "Sale",
                    id);
            }

            if (sale.Status != SaleStatus.Completed)
            {
                throw new ValidationException(
                    "Only completed sales can be updated with this method.");
            }

            /*
             * Customer correction is intentionally blocked here.
             * Customer changes should use the dedicated support action.
             */
            if (sale.CustomerId != request.CustomerId)
            {
                throw new ValidationException(
                    "Use the support customer-correction action " +
                    "to change the customer of a completed sale.");
            }

            if (request.Lines == null ||
                request.Lines.Count == 0)
            {
                throw new ValidationException(
                    "Sale must contain at least one line.");
            }

            // ============================================================
            // VALIDATE REQUEST LINES BEFORE CHANGING ANYTHING
            // ============================================================

            foreach (var line in request.Lines)
            {
                if (line.ProductId == Guid.Empty)
                {
                    throw new ValidationException(
                        "Every sale line requires a ProductId.");
                }

                if (line.Quantity <= 0)
                {
                    throw new ValidationException(
                        $"Quantity must be greater than 0 " +
                        $"for product {line.ProductId}.");
                }

                if (line.UnitPrice < 0)
                {
                    throw new ValidationException(
                        $"Unit price cannot be negative " +
                        $"for product {line.ProductId}.");
                }

                if (line.DiscountPercent < 0 ||
                    line.DiscountPercent > 100)
                {
                    throw new ValidationException(
                        $"Discount percent must be between 0 and 100 " +
                        $"for product {line.ProductId}.");
                }

                if (line.VatRate < 0 ||
                    line.VatRate > 100)
                {
                    throw new ValidationException(
                        $"VAT rate must be between 0 and 100 " +
                        $"for product {line.ProductId}.");
                }
            }

            var now =
                DateTime.UtcNow;

            // ============================================================
            // 1. REVERT PREVIOUS STOCK
            // ============================================================

            var movements =
                await _stockMovementRepository.GetAsync(
                    movement =>
                        movement.ReferenceId == sale.Id &&
                        movement.Type == StockMovementType.Sale &&
                        !movement.IsDeleted &&
                        movement.TenantId == tenantId);

            foreach (var movement in movements)
            {
                var stock =
                    await _stockRepository.GetSingleAsync(
                        item =>
                            item.ProductId ==
                                movement.ProductId &&
                            !item.IsDeleted &&
                            item.TenantId ==
                                tenantId);

                if (stock == null)
                {
                    throw new NotFoundException(
                        "Stock",
                        movement.ProductId);
                }

                /*
                 * Sale movements are negative.
                 *
                 * Math.Abs allows the old sold quantity
                 * to be restored.
                 */
                stock.Quantity +=
                    Math.Abs(
                        movement.QuantityChange);

                stock.LastUpdated =
                    now;

                stock.ModifiedAt =
                    now;

                _stockRepository.Update(
                    stock);

                _stockMovementRepository.Delete(
                    movement);
            }

            // ============================================================
            // 2. DELETE OLD SIDE EFFECTS
            // ============================================================

            var payments =
                await _paymentRepository.GetAsync(
                    payment =>
                        payment.SaleId ==
                            sale.Id);

            foreach (var payment in payments)
            {
                _paymentRepository.Delete(
                    payment);
            }

            var cashMovements =
                await _cashMovementRepository.GetAsync(
                    movement =>
                        movement.ReferenceId ==
                            sale.Id &&
                        movement.ReferenceType ==
                            "Sale" &&
                        movement.TenantId ==
                            tenantId);

            foreach (var cashMovement in cashMovements)
            {
                _cashMovementRepository.Delete(
                    cashMovement);
            }

            var customerTransactions =
                await _customerTransactionRepository.GetAsync(
                    transaction =>
                        transaction.SaleId ==
                            sale.Id &&
                        transaction.TenantId ==
                            tenantId);

            foreach (var transaction in customerTransactions)
            {
                _customerTransactionRepository.Delete(
                    transaction);
            }

            var existingLines =
                await _saleLineRepository.GetAsync(
                    line =>
                        line.SaleId ==
                            sale.Id);

            foreach (var existingLine in existingLines)
            {
                _saleLineRepository.Delete(
                    existingLine);
            }

            // ============================================================
            // 3. LOAD NEW PRODUCTS
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
                        productIds.Contains(
                            product.Id) &&
                        product.TenantId ==
                            tenantId &&
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
            // 4. RESOLVE CATALOG PACKS
            // ============================================================

            /*
             * IMPORTANT:
             *
             * Custom products have:
             *
             * CatalogProductId = null
             *
             * They must never be passed to PackService.
             */
            var catalogIds =
                products
                    .Where(product =>
                        product.CatalogProductId.HasValue &&
                        product.CatalogProductId.Value !=
                            Guid.Empty)
                    .Select(product =>
                        product.CatalogProductId!.Value)
                    .Distinct()
                    .ToList();

            List<ProductCatalog> packCatalogs;

            if (catalogIds.Count == 0)
            {
                packCatalogs =
                    new List<ProductCatalog>();
            }
            else
            {
                packCatalogs =
                    await _productCatalogRepository.Query()
                        .Include(catalog =>
                            catalog.PackComponents)
                        .Where(catalog =>
                            catalogIds.Contains(
                                catalog.Id) &&
                            catalog.IsPack &&
                            !catalog.IsDeleted)
                        .ToListAsync();
            }

            var componentCatalogIds =
                packCatalogs
                    .SelectMany(catalog =>
                        catalog.PackComponents)
                    .Select(component =>
                        component.ComponentCatalogId)
                    .Where(componentCatalogId =>
                        componentCatalogId !=
                            Guid.Empty)
                    .Distinct()
                    .ToList();

            // ============================================================
            // 5. LOAD UNIT PRODUCTS FOR PACKS
            // ============================================================

            List<Product> unitProducts;

            if (componentCatalogIds.Count == 0)
            {
                unitProducts =
                    new List<Product>();
            }
            else
            {
                unitProducts =
                    await _productRepository.Query()
                        .Include(product =>
                            product.CatalogProduct)
                        .Where(product =>
                            product.CatalogProductId.HasValue &&
                            componentCatalogIds.Contains(
                                product.CatalogProductId.Value) &&
                            product.TenantId ==
                                tenantId &&
                            !product.IsDeleted)
                        .ToListAsync();
            }

            var unitProductMap =
                unitProducts
                    .Where(product =>
                        product.CatalogProductId.HasValue &&
                        product.CatalogProductId.Value !=
                            Guid.Empty)
                    .GroupBy(product =>
                        product.CatalogProductId!.Value)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.First());

            // ============================================================
            // 6. RESOLVE SALE LINES
            // ============================================================

            var lineResolutions =
                new List<(
                    UpdateSaleLineRequest Line,
                    Guid StockProductId,
                    decimal StockQuantity,
                    bool IsPack,
                    decimal PackSize)>();

            foreach (var line in request.Lines)
            {
                var product =
                    productMap[
                        line.ProductId];

                // ========================================================
                // CASE 1: CUSTOM PRODUCT
                // ========================================================

                if (!product.CatalogProductId.HasValue ||
                    product.CatalogProductId.Value ==
                        Guid.Empty)
                {
                    lineResolutions.Add(
                        (
                            line,
                            product.Id,
                            line.Quantity,
                            false,
                            1m
                        ));

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
                    lineResolutions.Add(
                        (
                            line,
                            product.Id,
                            line.Quantity,
                            false,
                            1m
                        ));

                    continue;
                }

                // ========================================================
                // CASE 3: PACK PRODUCT
                // ========================================================

                var componentCatalogId =
                    _packService
                        .GetComponentCatalogId(
                            catalogId);

                if (!componentCatalogId.HasValue ||
                    componentCatalogId.Value ==
                        Guid.Empty)
                {
                    throw new ValidationException(
                        $"Pack configuration is invalid " +
                        $"for catalog '{catalogId}'.");
                }

                if (!unitProductMap.TryGetValue(
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
                        $"Pack size is invalid for " +
                        $"catalog '{catalogId}'.");
                }

                var unitQuantity =
                    _packService
                        .GetUnitQuantity(
                            catalogId,
                            line.Quantity);

                if (unitQuantity <= 0)
                {
                    throw new ValidationException(
                        $"Calculated unit quantity is invalid " +
                        $"for product '{product.Id}'.");
                }

                lineResolutions.Add(
                    (
                        line,
                        unitProduct.Id,
                        unitQuantity,
                        true,
                        packSize
                    ));
            }

            // ============================================================
            // 7. VALIDATE STOCK
            // ============================================================

            var stockIds =
                lineResolutions
                    .Select(resolution =>
                        resolution.StockProductId)
                    .Distinct()
                    .ToList();

            var stocks =
                await _stockRepository.GetAsync(
                    stock =>
                        stockIds.Contains(
                            stock.ProductId) &&
                        !stock.IsDeleted &&
                        stock.TenantId ==
                            tenantId);

            var stockMap =
                stocks.ToDictionary(
                    stock =>
                        stock.ProductId);

            var requiredByProduct =
                lineResolutions
                    .GroupBy(resolution =>
                        resolution.StockProductId)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.Sum(
                                resolution =>
                                    resolution.StockQuantity));

            foreach (var requirement
                     in requiredByProduct)
            {
                if (!stockMap.TryGetValue(
                        requirement.Key,
                        out var stock))
                {
                    throw new NotFoundException(
                        "Stock",
                        requirement.Key);
                }

                if (stock.Quantity <
                    requirement.Value)
                {
                    throw new ValidationException(
                        $"Insufficient stock for product " +
                        $"{requirement.Key}. " +
                        $"Required: {requirement.Value}, " +
                        $"Available: {stock.Quantity}.");
                }
            }

            // ============================================================
            // 8. APPLY NEW STOCK
            // ============================================================

            var newStockMovements =
                new List<StockMovement>();

            foreach (var requirement
                     in requiredByProduct)
            {
                var stock =
                    stockMap[
                        requirement.Key];

                var quantity =
                    Math.Round(
                        requirement.Value,
                        3,
                        MidpointRounding.AwayFromZero);

                var quantityBefore =
                    stock.Quantity;

                var quantityAfter =
                    quantityBefore -
                    quantity;

                if (quantityAfter < 0)
                {
                    throw new ValidationException(
                        $"Insufficient stock for product " +
                        $"{requirement.Key}.");
                }

                stock.Quantity =
                    quantityAfter;

                stock.LastUpdated =
                    now;

                stock.ModifiedAt =
                    now;

                _stockRepository.Update(
                    stock);

                newStockMovements.Add(
                    new StockMovement
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        ProductId =
                            requirement.Key,

                        Type =
                            StockMovementType.Sale,

                        QuantityChange =
                            -quantity,

                        QuantityBefore =
                            quantityBefore,

                        QuantityAfter =
                            quantityAfter,

                        ReferenceId =
                            sale.Id,

                        ReferenceNumber =
                            sale.InvoiceNumber,

                        MovementDate =
                            now,

                        Notes =
                            $"Sale {sale.InvoiceNumber}",

                        CreatedAt =
                            now,

                        ModifiedAt =
                            now
                    });
            }

            await _stockMovementRepository
                .AddRangeAsync(
                    newStockMovements);

            // ============================================================
            // 9. CALCULATE TOTALS
            // ============================================================

            decimal subtotal =
                0m;

            decimal vat =
                0m;

            decimal total =
                0m;

            foreach (var resolution
                     in lineResolutions)
            {
                var line =
                    resolution.Line;

                var gross =
                    line.Quantity *
                    line.UnitPrice;

                var discount =
                    gross *
                    (line.DiscountPercent /
                     100m);

                var net =
                    gross -
                    discount;

                var divisor =
                    1m +
                    (line.VatRate /
                     100m);

                var amountExclVat =
                    divisor <= 0
                        ? net
                        : net /
                          divisor;

                var vatAmount =
                    net -
                    amountExclVat;

                subtotal +=
                    Math.Round(
                        amountExclVat,
                        2,
                        MidpointRounding.AwayFromZero);

                vat +=
                    Math.Round(
                        vatAmount,
                        2,
                        MidpointRounding.AwayFromZero);

                total +=
                    Math.Round(
                        net,
                        2,
                        MidpointRounding.AwayFromZero);
            }

            subtotal =
                Math.Round(
                    subtotal,
                    2,
                    MidpointRounding.AwayFromZero);

            vat =
                Math.Round(
                    vat,
                    2,
                    MidpointRounding.AwayFromZero);

            total =
                Math.Round(
                    total,
                    2,
                    MidpointRounding.AwayFromZero);

            // ============================================================
            // 10. CREATE NEW SALE LINES
            // ============================================================

            var allResolvedProducts =
                products
                    .Concat(
                        unitProducts)
                    .GroupBy(product =>
                        product.Id)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.First());

            var newLines =
                lineResolutions
                    .Select(resolution =>
                    {
                        var line =
                            resolution.Line;

                        if (resolution.StockQuantity <= 0)
                        {
                            throw new ValidationException(
                                $"Invalid stock quantity for product " +
                                $"{resolution.StockProductId}.");
                        }

                        if (!allResolvedProducts.TryGetValue(
                                resolution.StockProductId,
                                out var stockProduct))
                        {
                            throw new NotFoundException(
                                "Product",
                                resolution.StockProductId);
                        }

                        var gross =
                            line.Quantity *
                            line.UnitPrice;

                        var discount =
                            gross *
                            (line.DiscountPercent /
                             100m);

                        /*
                         * StockProduct is already the actual unit
                         * product for a pack.
                         *
                         * Therefore PurchasePrice is already the
                         * unit cost. Do not divide by PackSize.
                         */
                        var unitCostPrice =
                            stockProduct.PurchasePrice;

                        return new SaleLine
                        {
                            Id =
                                Guid.NewGuid(),

                            TenantId =
                                tenantId,

                            SaleId =
                                sale.Id,

                            /*
                             * Product actually sold.
                             */
                            ProductId =
                                line.ProductId,

                            Quantity =
                                line.Quantity,

                            /*
                             * Product whose stock is affected.
                             *
                             * Custom:
                             * ProductId == UnitProductId
                             *
                             * Normal catalog product:
                             * ProductId == UnitProductId
                             *
                             * Pack:
                             * ProductId = pack
                             * UnitProductId = component product
                             */
                            UnitProductId =
                                resolution.StockProductId,

                            UnitQuantity =
                                resolution.StockQuantity,

                            UnitPrice =
                                line.UnitPrice,

                            VatRate =
                                line.VatRate,

                            DiscountPercent =
                                line.DiscountPercent,

                            DiscountAmount =
                                Math.Round(
                                    discount,
                                    2,
                                    MidpointRounding.AwayFromZero),

                            UnitCostPrice =
                                Math.Round(
                                    unitCostPrice,
                                    4,
                                    MidpointRounding.AwayFromZero),

                            CreatedAt =
                                now,

                            ModifiedAt =
                                now
                        };
                    })
                    .ToList();

            await _saleLineRepository
                .AddRangeAsync(
                    newLines);

            // ============================================================
            // 11. UPDATE SALE
            // ============================================================

            sale.SubtotalAmount =
                subtotal;

            sale.VatAmount =
                vat;

            sale.TotalAmount =
                total;

            sale.CustomerId =
                request.CustomerId;

            sale.Notes =
                request.Notes;

            sale.Status =
                SaleStatus.Completed;

            sale.PaymentStatus =
                request.PaymentStatus;

            sale.PaidAmount =
                Math.Round(
                    request.PaidAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            sale.ChangeAmount =
                Math.Round(
                    request.ChangeAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            sale.ModifiedAt =
                now;

            _repository.Update(
                sale);

            // ============================================================
            // 12. SAVE EVERYTHING
            // ============================================================

            await _unitOfWork
                .SaveChangesAsync();

            return _mapper.Map<SaleResult>(
                sale);
        }

        public async Task<SaleTicketResult> BuildTicketAsync(Guid saleId)
        {
            var tenantId = _tenantContext.TenantId;

            var sale = await _repository.GetByIdAsync(saleId);
            if (sale == null || sale.TenantId != tenantId)
                throw new NotFoundException("Sale", saleId);

            var lines = await _saleLineRepository.GetAsync(
                l => l.SaleId == saleId,
                l => l.Product
            );

            var payments = await _paymentRepository.GetAsync(
                p => p.SaleId == saleId
            );

            Customer? customer = null;
            if (sale.CustomerId != null)
                customer = await _customerRepository.GetByIdAsync(sale.CustomerId.Value);

            // ── Récupérer les infos du magasin depuis le Tenant ──────────
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);

            return new SaleTicketResult
            {
                InvoiceNumber = sale.InvoiceNumber,
                SaleDate = sale.SaleDate,

                // Infos magasin
                StoreName = tenant?.Name ?? "",
                StoreAddress = tenant?.Address,
                StoreCity = tenant?.City,
                StorePostalCode = tenant?.PostalCode,
                StorePhone = tenant?.Phone,
                StoreTaxNumber = tenant?.TaxNumber,
                ReceiptHeader = tenant?.ReceiptHeader,
                ReceiptFooter = tenant?.ReceiptFooter,

                // Client
                CustomerId = sale.CustomerId ?? Guid.Empty,
                CustomerName = customer?.Name ?? "Walk-in customer",

                Lines = lines.Select(l => new SaleTicketLineResult
                {
                    ProductName = l.Product.Name,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice
                }).ToList(),

                Payments = payments.Select(p => new TicketPaymentLine
                {
                    Method = p.Method.ToString(),
                    Amount = p.Amount
                }).ToList(),

                Subtotal = sale.SubtotalAmount,
                VatAmount = sale.VatAmount,
                Total = sale.TotalAmount,
                Paid = sale.PaidAmount,
                Change = sale.ChangeAmount,
            };
        }


        public async Task<SaleResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var sale = await _repository.GetByIdAsync(id);

            if (sale == null || sale.IsDeleted || sale.TenantId != tenantId)
                throw new NotFoundException("Sale", id);

            return _mapper.Map<SaleResult>(sale);
        }


        // =========================
        // Get Sales By Client
        // =========================
        public async Task<PagedResult<SaleResult>> GetByCustomerAsync(CustomerSaleQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1)
                throw new ValidationException("Page must be >= 1");
            if (query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException("PageSize must be between 1 and 100");

            // ← BLOQUER si aucun CustomerId fourni
            if (!query.CustomerId.HasValue)
                return new PagedResult<SaleResult>
                {
                    Items = new List<SaleResult>(),
                    TotalCount = 0,
                    Page = query.Page,
                    PageSize = query.PageSize
                };

            var sales = _repository.Query()
                .Where(s =>
                    !s.IsDeleted &&
                    s.TenantId == tenantId &&
                    s.CustomerId == query.CustomerId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                sales = sales.Where(s => s.InvoiceNumber.Contains(search));
            }

            var total = await sales.CountAsync();

            var items = await sales
                .OrderByDescending(s => s.SaleDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<SaleResult>
            {
                Items = _mapper.Map<List<SaleResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<SaleResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var sales = await _repository.GetAsync(
                s => !s.IsDeleted && s.TenantId == tenantId);

            return _mapper.Map<List<SaleResult>>(sales);
        }

        // =========================
        // PENDING
        // =========================
        public async Task<SaleResult> CreatePendingAsync(CreatePendingSaleRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var activeCashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

            decimal subtotalAmount = 0m;
            decimal vatAmount = 0m;
            decimal totalAmount = 0m;

            foreach (var line in request.SaleLines)
            {
                var lineGross = line.Quantity * line.UnitPrice; // TTC
                var lineDiscount = lineGross * (line.DiscountPercent / 100m);
                var lineNetTtc = lineGross - lineDiscount;

                var divisor = 1m + (line.VatRate / 100m);
                var lineHt = divisor <= 0 ? lineNetTtc : lineNetTtc / divisor;
                var lineVat = lineNetTtc - lineHt;

                subtotalAmount += Math.Round(lineHt, 2, MidpointRounding.AwayFromZero);
                vatAmount += Math.Round(lineVat, 2, MidpointRounding.AwayFromZero);
                totalAmount += Math.Round(lineNetTtc, 2, MidpointRounding.AwayFromZero);
            }

            subtotalAmount = Math.Round(subtotalAmount, 2, MidpointRounding.AwayFromZero);
            vatAmount = Math.Round(vatAmount, 2, MidpointRounding.AwayFromZero);
            totalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero);

            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InvoiceNumber = await _documentNumberService.GenerateAsync("191125"),
                CustomerId = request.CustomerId,
                SaleDate = request.SaleDate ?? DateTime.UtcNow,
                CashSessionId = activeCashSessionId,
                TotalAmount = totalAmount,
                SubtotalAmount = subtotalAmount,
                VatAmount = vatAmount,
                Status = SaleStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            sale.Lines = new List<SaleLine>();

            foreach (var lineItem in request.SaleLines)
            {
                var product = await _productRepository
                    .Query()
                    .Include(p => p.CatalogProduct)
                        .ThenInclude(c => c.PackComponents)
                    .FirstAsync(p => p.Id == lineItem.ProductId);

                var lineGross = lineItem.Quantity * lineItem.UnitPrice;
                var lineDiscount = lineGross * (lineItem.DiscountPercent / 100m);

                Guid unitProductId;

                if (product.CatalogProduct?.IsPack == true)
                {
                    var componentCatalogId = product.CatalogProduct
                        .PackComponents
                        .First()
                        .ComponentCatalogId;

                    var unitProduct = await _productRepository
                        .Query()
                        .FirstOrDefaultAsync(p =>
                            p.CatalogProductId == componentCatalogId &&
                            p.TenantId == tenantId &&
                            !p.IsDeleted);

                    if (unitProduct == null)
                        throw new ValidationException($"Unit product not found for catalog {componentCatalogId}");

                    unitProductId = unitProduct.Id;
                }
                else
                {
                    unitProductId = lineItem.ProductId;
                }

                sale.Lines.Add(new SaleLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,

                    ProductId = lineItem.ProductId,
                    UnitProductId = unitProductId,

                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    VatRate = lineItem.VatRate,
                    DiscountPercent = lineItem.DiscountPercent,
                    DiscountAmount = Math.Round(lineDiscount, 2, MidpointRounding.AwayFromZero),

                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });
            }

            await _repository.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(sale);
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<SaleResult> UpdateAsync(
    Guid id,
    UpdateSaleRequest request)
        {
            if (request == null)
            {
                throw new ValidationException(
                    "Request cannot be null.");
            }

            if (id == Guid.Empty)
            {
                throw new ValidationException(
                    "Sale ID is required.");
            }

            var tenantId =
                _tenantContext.TenantId;

            // ============================================================
            // LOAD PENDING SALE
            // ============================================================

            var sale =
                await _repository.Query()
                    .Include(s =>
                        s.Lines)
                    .FirstOrDefaultAsync(s =>
                        s.Id == id &&
                        !s.IsDeleted &&
                        s.TenantId == tenantId);

            if (sale == null)
            {
                throw new NotFoundException(
                    "Sale",
                    id);
            }

            if (sale.Status != SaleStatus.Pending)
            {
                throw new ValidationException(
                    "Only pending sales can be updated with this method.");
            }

            if (request.Lines == null ||
                request.Lines.Count == 0)
            {
                throw new ValidationException(
                    "Sale must contain at least one line.");
            }

            // ============================================================
            // VALIDATE LINES
            // ============================================================

            foreach (var line in request.Lines)
            {
                if (line.ProductId == Guid.Empty)
                {
                    throw new ValidationException(
                        "Every sale line requires a ProductId.");
                }

                if (line.Quantity <= 0)
                {
                    throw new ValidationException(
                        $"Quantity must be greater than 0 " +
                        $"for product {line.ProductId}.");
                }

                if (line.UnitPrice < 0)
                {
                    throw new ValidationException(
                        $"Unit price cannot be negative " +
                        $"for product {line.ProductId}.");
                }

                if (line.DiscountPercent < 0 ||
                    line.DiscountPercent > 100)
                {
                    throw new ValidationException(
                        $"Discount percent must be between 0 and 100 " +
                        $"for product {line.ProductId}.");
                }

                if (line.VatRate < 0 ||
                    line.VatRate > 100)
                {
                    throw new ValidationException(
                        $"VAT rate must be between 0 and 100 " +
                        $"for product {line.ProductId}.");
                }
            }

            // ============================================================
            // DELETE OLD LINES
            // ============================================================

            var existingLines =
                await _saleLineRepository.GetAsync(
                    line =>
                        line.SaleId == id);

            foreach (var existingLine in existingLines)
            {
                _saleLineRepository.Delete(
                    existingLine);
            }

            // ============================================================
            // PRELOAD PRODUCTS
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
                        productIds.Contains(
                            product.Id) &&
                        product.TenantId ==
                            tenantId &&
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
            // LOAD PACK COMPONENT PRODUCTS
            // ============================================================

            /*
             * Only real catalog-linked products participate
             * in pack resolution.
             *
             * Custom products have CatalogProductId = null.
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
                    .Where(componentCatalogId =>
                        componentCatalogId.HasValue &&
                        componentCatalogId.Value != Guid.Empty)
                    .Select(componentCatalogId =>
                        componentCatalogId!.Value)
                    .Distinct()
                    .ToList();

            List<Product> tenantProducts;

            if (componentCatalogIds.Count == 0)
            {
                tenantProducts =
                    new List<Product>();
            }
            else
            {
                tenantProducts =
                    await _productRepository.Query()
                        .Include(product =>
                            product.CatalogProduct)
                        .Where(product =>
                            product.CatalogProductId.HasValue &&
                            componentCatalogIds.Contains(
                                product.CatalogProductId.Value) &&
                            product.TenantId ==
                                tenantId &&
                            !product.IsDeleted)
                        .ToListAsync();
            }

            var unitProductMap =
                tenantProducts
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

            // ============================================================
            // BUILD NEW SALE LINES
            // ============================================================

            var newLines =
                new List<SaleLine>();

            decimal subtotalAmount =
                0m;

            decimal vatAmount =
                0m;

            decimal totalAmount =
                0m;

            var now =
                DateTime.UtcNow;

            foreach (var line in request.Lines)
            {
                var product =
                    productMap[
                        line.ProductId];

                Guid unitProductId;

                decimal unitQuantity;

                // ========================================================
                // CASE 1: CUSTOM PRODUCT
                // ========================================================

                if (!product.CatalogProductId.HasValue ||
                    product.CatalogProductId.Value ==
                        Guid.Empty)
                {
                    unitProductId =
                        product.Id;

                    unitQuantity =
                        line.Quantity;
                }
                else
                {
                    var catalogId =
                        product.CatalogProductId.Value;

                    // ====================================================
                    // CASE 2: NORMAL CATALOG PRODUCT
                    // ====================================================

                    if (!_packService.IsPack(
                            catalogId))
                    {
                        unitProductId =
                            product.Id;

                        unitQuantity =
                            line.Quantity;
                    }
                    else
                    {
                        // ================================================
                        // CASE 3: PACK
                        // ================================================

                        var componentCatalogId =
                            _packService
                                .GetComponentCatalogId(
                                    catalogId);

                        if (!componentCatalogId.HasValue ||
                            componentCatalogId.Value ==
                                Guid.Empty)
                        {
                            throw new ValidationException(
                                $"Pack configuration is invalid " +
                                $"for catalog '{catalogId}'.");
                        }

                        if (!unitProductMap.TryGetValue(
                                componentCatalogId.Value,
                                out var unitProduct))
                        {
                            throw new NotFoundException(
                                "Unit product",
                                componentCatalogId.Value);
                        }

                        unitProductId =
                            unitProduct.Id;

                        unitQuantity =
                            _packService
                                .GetUnitQuantity(
                                    catalogId,
                                    line.Quantity);

                        if (unitQuantity <= 0)
                        {
                            throw new ValidationException(
                                $"Calculated unit quantity is invalid " +
                                $"for product '{product.Id}'.");
                        }
                    }
                }

                // ========================================================
                // TOTALS
                // ========================================================

                var lineGross =
                    line.Quantity *
                    line.UnitPrice;

                var lineDiscount =
                    lineGross *
                    (line.DiscountPercent /
                     100m);

                var lineNetTtc =
                    lineGross -
                    lineDiscount;

                var divisor =
                    1m +
                    (line.VatRate /
                     100m);

                var lineHt =
                    divisor <= 0
                        ? lineNetTtc
                        : lineNetTtc /
                          divisor;

                var lineVat =
                    lineNetTtc -
                    lineHt;

                subtotalAmount +=
                    Math.Round(
                        lineHt,
                        2,
                        MidpointRounding.AwayFromZero);

                vatAmount +=
                    Math.Round(
                        lineVat,
                        2,
                        MidpointRounding.AwayFromZero);

                totalAmount +=
                    Math.Round(
                        lineNetTtc,
                        2,
                        MidpointRounding.AwayFromZero);

                // ========================================================
                // COST PRICE
                // ========================================================

                Product stockProduct;

                if (unitProductId == product.Id)
                {
                    stockProduct =
                        product;
                }
                else
                {
                    stockProduct =
                        tenantProducts.FirstOrDefault(
                            item =>
                                item.Id ==
                                    unitProductId)
                        ?? throw new NotFoundException(
                            "Unit product",
                            unitProductId);
                }

                var unitCostPrice =
                    stockProduct.PurchasePrice;

                // ========================================================
                // SALE LINE
                // ========================================================

                newLines.Add(
                    new SaleLine
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        SaleId =
                            sale.Id,

                        ProductId =
                            line.ProductId,

                        Quantity =
                            line.Quantity,

                        /*
                         * For custom and normal products:
                         * UnitProductId == ProductId.
                         *
                         * For packs:
                         * UnitProductId == component Product.
                         */
                        UnitProductId =
                            unitProductId,

                        UnitQuantity =
                            unitQuantity,

                        UnitPrice =
                            line.UnitPrice,

                        VatRate =
                            line.VatRate,

                        DiscountPercent =
                            line.DiscountPercent,

                        DiscountAmount =
                            Math.Round(
                                lineDiscount,
                                2,
                                MidpointRounding.AwayFromZero),

                        UnitCostPrice =
                            Math.Round(
                                unitCostPrice,
                                4,
                                MidpointRounding.AwayFromZero),

                        CreatedAt =
                            now,

                        ModifiedAt =
                            now
                    });
            }

            // ============================================================
            // NORMALIZE TOTALS
            // ============================================================

            subtotalAmount =
                Math.Round(
                    subtotalAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            vatAmount =
                Math.Round(
                    vatAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            totalAmount =
                Math.Round(
                    totalAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            // ============================================================
            // SAVE LINES
            // ============================================================

            await _saleLineRepository
                .AddRangeAsync(
                    newLines);

            // ============================================================
            // UPDATE SALE
            // ============================================================

            sale.CustomerId =
                request.CustomerId;

            if (request.SaleDate != default)
            {
                sale.SaleDate =
                    EnsureUtc(
                        request.SaleDate);
            }

            sale.SubtotalAmount =
                subtotalAmount;

            sale.VatAmount =
                vatAmount;

            sale.TotalAmount =
                totalAmount;

            sale.Notes =
                request.Notes;

            sale.ModifiedAt =
                now;

            /*
             * This method updates only a pending sale.
             * It does NOT affect stock yet.
             */
            sale.Status =
                SaleStatus.Pending;

            sale.PaymentStatus =
                PaymentStatus.Pending;

            sale.PaidAmount =
                0m;

            sale.ChangeAmount =
                0m;

            _repository.Update(
                sale);

            // ============================================================
            // SAVE
            // ============================================================

            await _unitOfWork
                .SaveChangesAsync();

            return _mapper.Map<SaleResult>(
                sale);
        }

        // =========================
        // SOFT DELETE
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var sale = await _repository.GetByIdAsync(id);

            if (sale == null || sale.IsDeleted || sale.TenantId != tenantId)
                throw new NotFoundException("Sale", id);

            sale.IsDeleted = true;
            sale.ModifiedAt = DateTime.UtcNow;

            _repository.Update(sale);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // Get Pending
        // =========================
        public async Task<List<SaleResult>> GetPendingAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var sales = await _repository.Query()
                .Include(s => s.Lines)
                .Where(s =>
                    s.Status == SaleStatus.Pending &&
                    !s.IsDeleted &&
                    s.TenantId == tenantId &&
                    s.Lines.Any())
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<SaleResult>>(sales);
        }

        // =========================
        // Get Pending By Id
        // =========================
        public async Task<SaleResult> GetPendingByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var sale = await _repository.Query()
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.Status == SaleStatus.Pending &&
                    !s.IsDeleted &&
                    s.TenantId == tenantId &&
                    s.Lines.Any());

            if (sale == null)
                throw new NotFoundException("Sale", id);

            return _mapper.Map<SaleResult>(sale);
        }

        public async Task<SaleResult> UpdatePendingAsync(
     Guid id,
     CreatePendingSaleRequest request)
        {
            if (request == null)
            {
                throw new ValidationException(
                    "Request cannot be null.");
            }

            if (id == Guid.Empty)
            {
                throw new ValidationException(
                    "Sale ID is required.");
            }

            if (request.SaleLines == null ||
                request.SaleLines.Count == 0)
            {
                throw new ValidationException(
                    "Pending sale must contain at least one line.");
            }

            var tenantId =
                _tenantContext.TenantId;

            // ============================================================
            // LOAD PENDING SALE
            // ============================================================

            var sale =
                await _repository.Query()
                    .Include(s =>
                        s.Lines)
                    .FirstOrDefaultAsync(s =>
                        s.Id == id &&
                        s.Status == SaleStatus.Pending &&
                        !s.IsDeleted &&
                        s.TenantId == tenantId);

            if (sale == null)
            {
                throw new NotFoundException(
                    "Pending Sale",
                    id);
            }

            // ============================================================
            // VALIDATE REQUEST LINES
            // ============================================================

            foreach (var line in request.SaleLines)
            {
                if (line.ProductId == Guid.Empty)
                {
                    throw new ValidationException(
                        "Every sale line requires a ProductId.");
                }

                if (line.Quantity <= 0)
                {
                    throw new ValidationException(
                        $"Quantity must be greater than 0 " +
                        $"for product {line.ProductId}.");
                }

                if (line.UnitPrice < 0)
                {
                    throw new ValidationException(
                        $"Unit price cannot be negative " +
                        $"for product {line.ProductId}.");
                }

                if (line.DiscountPercent < 0 ||
                    line.DiscountPercent > 100)
                {
                    throw new ValidationException(
                        $"Discount percent must be between 0 and 100 " +
                        $"for product {line.ProductId}.");
                }

                if (line.VatRate < 0 ||
                    line.VatRate > 100)
                {
                    throw new ValidationException(
                        $"VAT rate must be between 0 and 100 " +
                        $"for product {line.ProductId}.");
                }
            }

            // ============================================================
            // DELETE OLD LINES
            // ============================================================

            var existingLines =
                await _saleLineRepository.GetAsync(
                    line =>
                        line.SaleId == sale.Id);

            foreach (var line in existingLines)
            {
                _saleLineRepository.Delete(
                    line);
            }

            /*
             * Keep this SaveChanges because your current implementation
             * persists removal of old pending lines before rebuilding them.
             */
            await _unitOfWork.SaveChangesAsync();

            // ============================================================
            // PRELOAD PRODUCTS
            // ============================================================

            var productIds =
                request.SaleLines
                    .Select(line =>
                        line.ProductId)
                    .Distinct()
                    .ToList();

            var products =
                await _productRepository.Query()
                    .Include(product =>
                        product.CatalogProduct)
                            .ThenInclude(catalog =>
                                catalog!.PackComponents)
                    .Where(product =>
                        productIds.Contains(
                            product.Id) &&
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
            // RESOLVE CATALOG IDS
            // ============================================================

            /*
             * Custom products have CatalogProductId = null.
             * They must not participate in PackService resolution.
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
                    .Where(componentCatalogId =>
                        componentCatalogId.HasValue &&
                        componentCatalogId.Value != Guid.Empty)
                    .Select(componentCatalogId =>
                        componentCatalogId!.Value)
                    .Distinct()
                    .ToList();

            // ============================================================
            // LOAD UNIT PRODUCTS FOR PACKS
            // ============================================================

            List<Product> allTenantProductsForCatalogs;

            if (componentCatalogIds.Count == 0)
            {
                allTenantProductsForCatalogs =
                    new List<Product>();
            }
            else
            {
                allTenantProductsForCatalogs =
                    await _productRepository.Query()
                        .Include(product =>
                            product.CatalogProduct)
                        .Where(product =>
                            product.CatalogProductId.HasValue &&
                            componentCatalogIds.Contains(
                                product.CatalogProductId.Value) &&
                            product.TenantId == tenantId &&
                            !product.IsDeleted)
                        .ToListAsync();
            }

            var unitProductMap =
                allTenantProductsForCatalogs
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

            // ============================================================
            // CREATE NEW LINES
            // ============================================================

            var newLines =
                new List<SaleLine>();

            decimal subtotalAmount =
                0m;

            decimal vatAmount =
                0m;

            decimal totalAmount =
                0m;

            var now =
                DateTime.UtcNow;

            foreach (var lineItem in request.SaleLines)
            {
                var product =
                    productMap[
                        lineItem.ProductId];

                Guid unitProductId;

                decimal unitQuantity;

                // ========================================================
                // CASE 1: CUSTOM PRODUCT
                // ========================================================

                if (!product.CatalogProductId.HasValue ||
                    product.CatalogProductId.Value == Guid.Empty)
                {
                    unitProductId =
                        product.Id;

                    unitQuantity =
                        lineItem.Quantity;
                }
                else
                {
                    var catalogId =
                        product.CatalogProductId.Value;

                    // ====================================================
                    // CASE 2: NORMAL CATALOG PRODUCT
                    // ====================================================

                    if (!_packService.IsPack(
                            catalogId))
                    {
                        unitProductId =
                            product.Id;

                        unitQuantity =
                            lineItem.Quantity;
                    }
                    else
                    {
                        // ================================================
                        // CASE 3: PACK PRODUCT
                        // ================================================

                        var componentCatalogId =
                            _packService
                                .GetComponentCatalogId(
                                    catalogId);

                        if (!componentCatalogId.HasValue ||
                            componentCatalogId.Value == Guid.Empty)
                        {
                            throw new ValidationException(
                                $"Pack configuration is invalid " +
                                $"for catalog '{catalogId}'.");
                        }

                        if (!unitProductMap.TryGetValue(
                                componentCatalogId.Value,
                                out var unitProduct))
                        {
                            throw new NotFoundException(
                                "Unit product",
                                componentCatalogId.Value);
                        }

                        unitProductId =
                            unitProduct.Id;

                        unitQuantity =
                            _packService
                                .GetUnitQuantity(
                                    catalogId,
                                    lineItem.Quantity);

                        if (unitQuantity <= 0)
                        {
                            throw new ValidationException(
                                $"Calculated unit quantity is invalid " +
                                $"for product '{product.Id}'.");
                        }
                    }
                }

                // ========================================================
                // RESOLVE COST PRODUCT
                // ========================================================

                Product stockProduct;

                if (unitProductId == product.Id)
                {
                    stockProduct =
                        product;
                }
                else
                {
                    stockProduct =
                        allTenantProductsForCatalogs
                            .FirstOrDefault(item =>
                                item.Id ==
                                    unitProductId)
                        ?? throw new NotFoundException(
                            "Unit product",
                            unitProductId);
                }

                // ========================================================
                // TOTALS
                // ========================================================

                var lineGross =
                    lineItem.Quantity *
                    lineItem.UnitPrice;

                var lineDiscount =
                    lineGross *
                    (lineItem.DiscountPercent /
                     100m);

                var lineNetTtc =
                    lineGross -
                    lineDiscount;

                var divisor =
                    1m +
                    (lineItem.VatRate /
                     100m);

                var lineAmountExclVat =
                    divisor <= 0
                        ? lineNetTtc
                        : lineNetTtc /
                          divisor;

                var lineVatAmount =
                    lineNetTtc -
                    lineAmountExclVat;

                subtotalAmount +=
                    Math.Round(
                        lineAmountExclVat,
                        2,
                        MidpointRounding.AwayFromZero);

                vatAmount +=
                    Math.Round(
                        lineVatAmount,
                        2,
                        MidpointRounding.AwayFromZero);

                totalAmount +=
                    Math.Round(
                        lineNetTtc,
                        2,
                        MidpointRounding.AwayFromZero);

                // ========================================================
                // CREATE SALE LINE
                // ========================================================

                newLines.Add(
                    new SaleLine
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        SaleId =
                            sale.Id,

                        /*
                         * Product actually selected/sold.
                         */
                        ProductId =
                            lineItem.ProductId,

                        /*
                         * Product whose stock will eventually be affected.
                         *
                         * Custom / normal:
                         * UnitProductId == ProductId
                         *
                         * Pack:
                         * UnitProductId == component Product
                         */
                        UnitProductId =
                            unitProductId,

                        Quantity =
                            lineItem.Quantity,

                        UnitQuantity =
                            unitQuantity,

                        UnitPrice =
                            lineItem.UnitPrice,

                        VatRate =
                            lineItem.VatRate,

                        DiscountPercent =
                            lineItem.DiscountPercent,

                        DiscountAmount =
                            Math.Round(
                                lineDiscount,
                                2,
                                MidpointRounding.AwayFromZero),

                        UnitCostPrice =
                            Math.Round(
                                stockProduct.PurchasePrice,
                                4,
                                MidpointRounding.AwayFromZero),

                        CreatedAt =
                            now,

                        ModifiedAt =
                            now
                    });
            }

            // ============================================================
            // NORMALIZE TOTALS
            // ============================================================

            subtotalAmount =
                Math.Round(
                    subtotalAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            vatAmount =
                Math.Round(
                    vatAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            totalAmount =
                Math.Round(
                    totalAmount,
                    2,
                    MidpointRounding.AwayFromZero);

            // ============================================================
            // SAVE NEW LINES
            // ============================================================

            await _saleLineRepository
                .AddRangeAsync(
                    newLines);

            // ============================================================
            // UPDATE PENDING SALE
            // ============================================================

            sale.CustomerId =
                request.CustomerId;

            sale.SaleDate =
                request.SaleDate.HasValue
                    ? EnsureUtc(
                        request.SaleDate.Value)
                    : sale.SaleDate;

            sale.SubtotalAmount =
                subtotalAmount;

            sale.VatAmount =
                vatAmount;

            sale.TotalAmount =
                totalAmount;

            sale.ModifiedAt =
                now;

            /*
             * Important:
             * this remains a pending sale.
             *
             * No stock deduction and no payment side effects happen here.
             */
            sale.Status =
                SaleStatus.Pending;

            sale.PaymentStatus =
                PaymentStatus.Pending;

            sale.PaidAmount =
                0m;

            sale.ChangeAmount =
                0m;

            _repository.Update(
                sale);

            // ============================================================
            // SAVE
            // ============================================================

            await _unitOfWork
                .SaveChangesAsync();

            return _mapper.Map<SaleResult>(
                sale);
        }
        // Add this method to SaleService.cs

        // =========================
        // SALES HISTORY
        // =========================
        public async Task<PagedResult<SaleResult>> GetHistoryAsync(SaleHistoryQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1)
                throw new ValidationException("Page must be >= 1");
            if (query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException("PageSize must be between 1 and 100");

            var sales = _repository.Query()
                .Where(s => !s.IsDeleted && s.TenantId == tenantId);

            // ── Invoice search ────────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(query.Search))
                sales = sales.Where(s => s.InvoiceNumber.Contains(query.Search.Trim()));

            // ── Date range ────────────────────────────────────────────────────────────
            if (query.DateFrom.HasValue)
                sales = sales.Where(s => s.SaleDate >= query.DateFrom.Value);

            if (query.DateTo.HasValue)
                sales = sales.Where(s => s.SaleDate <= query.DateTo.Value);

            // ── Customer filter ───────────────────────────────────────────────────────
            if (query.WalkInOnly)
            {
                // Walk-in = no customer linked
                sales = sales.Where(s => s.CustomerId == null);
            }
            else if (query.CustomerId.HasValue)
            {
                // Specific customer
                sales = sales.Where(s => s.CustomerId == query.CustomerId.Value);
            }
            // else: All → no customer filter applied

            // ── Payment status filter ─────────────────────────────────────────────────
            if (query.PaymentStatuses != null && query.PaymentStatuses.Any())
            {
                var statuses = query.PaymentStatuses
                    .Select(s => Enum.TryParse<PaymentStatus>(s, true, out var ps) ? ps : (PaymentStatus?)null)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();

                if (statuses.Any())
                    sales = sales.Where(s => statuses.Contains(s.PaymentStatus));
            }

            // ── Sale status filter ────────────────────────────────────────────────────
            if (query.SaleStatuses != null && query.SaleStatuses.Any())
            {
                var statuses = query.SaleStatuses
                    .Select(s => Enum.TryParse<SaleStatus>(s, true, out var ss) ? ss : (SaleStatus?)null)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();

                if (statuses.Any())
                    sales = sales.Where(s => statuses.Contains(s.Status));
            }

            // ── Count + paginate ──────────────────────────────────────────────────────
            var total = await sales.CountAsync();

            var items = await sales
                .OrderByDescending(s => s.SaleDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<SaleResult>
            {
                Items = _mapper.Map<List<SaleResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        // =========================
        // QUERY
        // =========================

        public async Task<PagedResult<SaleResult>> QueryAsync(SaleQuery query)
        {
            var tenantId = _tenantContext.TenantId;
            var search = query.Search?.Trim() ?? string.Empty;

            var all = _repository.Query()
                .Where(s => !s.IsDeleted
                         && s.TenantId == tenantId
                         && s.InvoiceNumber.Contains(search));

            var total = await all.CountAsync();

            var items = await all
                .OrderByDescending(s => s.SaleDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<SaleResult>
            {
                Items = _mapper.Map<List<SaleResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        private class LineResolution
        {
            public SaleLineItem OriginalLine { get; set; } = null!;
            public Guid StockProductId { get; set; }
            public decimal StockQuantity { get; set; }
            public bool IsPack { get; set; }
            public decimal PackSize { get; set; }
        }
    }
}
