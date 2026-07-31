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

        public async Task<SaleResult> CreateCompleteAsync(CreateCompleteSaleRequest request)
        {
            if (!_tenantContext.IsCashier && !_tenantContext.IsAdmin)
                throw new ForbiddenException("Only cashiers or admins can create sales.");

            var tenantId = _tenantContext.TenantId;

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

            Guid cashSessionId;

            if (request.CashSessionId.HasValue &&
                request.CashSessionId.Value != Guid.Empty)
            {
                /*
                 * Synchronisation d’une vente locale :
                 * conserver la session de caisse d’origine.
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

            if (cashSession is null)
            {
                throw new ValidationException(
                    $"Cash session '{cashSessionId}' was not found " +
                    "for the current tenant.");
            }

            if (request.Lines == null || !request.Lines.Any())
                throw new ValidationException("Sale must contain at least one line.");

            Sale sale;

            var effectivePendingSaleId = request.PendingSaleId ?? existingOperationSale?.Id;

            var isExistingPending = effectivePendingSaleId.HasValue;

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
                                sale.TenantId == tenantId)
                    ?? throw new NotFoundException(
                        "Pending Sale",
                        effectivePendingSaleId!.Value);

                if (sale.ClientOperationId == Guid.Empty)
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
                    await _saleLineRepository.GetAsync(
                        line =>
                            line.SaleId == sale.Id);

                foreach (var line in existingLines)
                {
                    _saleLineRepository.Delete(
                        line);
                }
            }
            else
            {
                sale = new Sale
                {
                    Id = Guid.NewGuid(),
                    ClientOperationId = request.ClientOperationId,
                    TenantId = tenantId,
                    InvoiceNumber = await _documentNumberService.GenerateAsync("191125"),
                    CashSessionId = cashSessionId,
                    SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                    Status = SaleStatus.Pending,
                    PaymentStatus = PaymentStatus.Pending
                };

                await _repository.AddAsync(sale);
                await _unitOfWork.SaveChangesAsync();
            }

            // ── Preload products to avoid N+1 queries ────────────────────────────────
            var requestProductIds = request.Lines
                .Select(l => l.ProductId)
                .Distinct()
                .ToList();

            var products = await _productRepository.Query()
                .Include(product => product.CatalogProduct)
                .Where(product =>
                    requestProductIds.Contains(product.Id) &&
                    product.TenantId == tenantId &&
                    !product.IsDeleted)
                .ToListAsync();

            var productMap = products.ToDictionary(p => p.Id);

            var catalogIds = products
                .Select(p => p.CatalogProductId)
                .Distinct()
                .ToList();

            var packCatalogs = await _productCatalogRepository.Query()
                .Include(c => c.PackComponents)
                .Where(c => catalogIds.Contains(c.Id) && c.IsPack)
                .ToListAsync();

            var componentCatalogIds = packCatalogs
                .SelectMany(c => c.PackComponents)
                .Select(pc => pc.ComponentCatalogId)
                .Distinct()
                .ToList();

            var allTenantProductsForCatalogs = await _productRepository.Query()
                .Include(p => p.CatalogProduct)
                .Where(p => componentCatalogIds.Contains(p.CatalogProductId)
                         && p.TenantId == tenantId
                         && !p.IsDeleted)
                .ToListAsync();

            var unitProductMap = allTenantProductsForCatalogs
                .Where(p => p.CatalogProduct != null && !p.CatalogProduct.IsPack)
                .GroupBy(p => p.CatalogProductId)
                .ToDictionary(g => g.Key, g => g.First());

            // ── Resolve sale lines (pack → unit stock product) ───────────────────────
            var lineResolutions = new List<LineResolution>();

            foreach (var line in request.Lines)
            {
                if (!productMap.TryGetValue(line.ProductId, out var product))
                    throw new NotFoundException("Product", line.ProductId);

                if (line.Quantity <= 0)
                    throw new ValidationException($"Quantity must be greater than 0 for product {line.ProductId}.");

                if (line.UnitPrice < 0)
                    throw new ValidationException($"UnitPrice cannot be negative for product {line.ProductId}.");

                if (line.DiscountPercent < 0 || line.DiscountPercent > 100)
                    throw new ValidationException($"DiscountPercent must be between 0 and 100 for product {line.ProductId}.");

                if (line.VatRate < 0 || line.VatRate > 100)
                    throw new ValidationException($"VatRate must be between 0 and 100 for product {line.ProductId}.");

                var catalogId = product.CatalogProductId;

                if (_packService.IsPack(catalogId))
                {
                    var componentCatalogId = _packService.GetComponentCatalogId(catalogId);
                    if (componentCatalogId == null || componentCatalogId == Guid.Empty)
                        throw new ValidationException($"Pack configuration is invalid for catalog {catalogId}.");

                    var unitQuantity = _packService.GetUnitQuantity(catalogId, line.Quantity);
                    var packSize = _packService.GetPackSize(catalogId);

                    if (!unitProductMap.TryGetValue(componentCatalogId.Value, out var unitProduct))
                        throw new ValidationException(
                            $"Unit product not found for catalog {componentCatalogId.Value}. " +
                            $"Pack configuration is broken.");

                    lineResolutions.Add(new LineResolution
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
                    lineResolutions.Add(new LineResolution
                    {
                        OriginalLine = line,
                        StockProductId = line.ProductId,
                        StockQuantity = line.Quantity,
                        IsPack = false,
                        PackSize = 1m
                    });
                }
            }

            // ── Validate stock ────────────────────────────────────────────────────────
            var resolvedProductIds = lineResolutions
                .Select(r => r.StockProductId)
                .Distinct()
                .ToList();

            var stocks = await _stockRepository.GetAsync(s =>
                resolvedProductIds.Contains(s.ProductId) &&
                !s.IsDeleted &&
                s.TenantId == tenantId);

            var stockMap = stocks.ToDictionary(s => s.ProductId);

            var requiredByProduct = lineResolutions
                .GroupBy(r => r.StockProductId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.StockQuantity));

            foreach (var requirement in requiredByProduct)
            {
                if (!stockMap.TryGetValue(requirement.Key, out var stock))
                    throw new NotFoundException("Stock", requirement.Key);

                if (stock.Quantity < requirement.Value)
                    throw new ValidationException(
                        $"Insufficient stock for product {requirement.Key}. " +
                        $"Required: {requirement.Value}, Available: {stock.Quantity}");
            }

            // ── Deduct stock + create stock movements ────────────────────────────────
            var stockMovements = new List<StockMovement>();

            foreach (var requirement in requiredByProduct)
            {
                var stock = stockMap[requirement.Key];
                var quantityBefore = stock.Quantity;
                var quantityAfter = quantityBefore - requirement.Value;

                stockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = requirement.Key,
                    Type = StockMovementType.Sale,
                    QuantityChange = -requirement.Value,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = quantityAfter,
                    ReferenceId = sale.Id,
                    ReferenceNumber = sale.InvoiceNumber,
                    MovementDate = DateTime.UtcNow,
                    Notes = $"Sale {sale.InvoiceNumber}",
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

                stock.Quantity = quantityAfter;
                stock.LastUpdated = DateTime.UtcNow;
                stock.ModifiedAt = DateTime.UtcNow;
                _stockRepository.Update(stock);
            }

            await _stockMovementRepository.AddRangeAsync(stockMovements);

            // ── Calculate totals from resolved lines ─────────────────────────────────
            decimal subtotalAmount = 0m;
            decimal vatAmount = 0m;
            decimal totalAmount = 0m;

            foreach (var resolution in lineResolutions)
            {
                var line = resolution.OriginalLine;

                var lineGross = line.Quantity * line.UnitPrice;
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

            // ── Validate payments ────────────────────────────────────────────────────
            var paidAmount = Math.Round(request.Payments?.Sum(p => p.Amount) ?? 0m, 2);

            if (paidAmount < 0)
                throw new ValidationException("Paid amount cannot be negative.");

            if (request.ChangeAmount < 0)
                throw new ValidationException("Change amount cannot be negative.");

            //if (paidAmount < request.ChangeAmount)
            //    throw new ValidationException("Change amount cannot exceed paid amount.");

            if ((paidAmount - request.ChangeAmount) > totalAmount + 0.01m)
                throw new ValidationException("Invalid payment / change combination.");

            var netTendered = Math.Round(paidAmount - request.ChangeAmount, 2, MidpointRounding.AwayFromZero);

            if (netTendered + 0.01m < totalAmount)
            {
                throw new ValidationException(
                    "The payment amount is lower than the sale total.");
            }

            // ── Persist payments + accumulate totals in ONE loop ─────────────────────
            decimal cashAmount = 0m;
            decimal cashAndCardPaid = 0m;
            decimal creditAmount = 0m;

            if (request.Payments != null && request.Payments.Any())
            {
                foreach (var paymentInfo in request.Payments)
                {
                    if (!Enum.TryParse<PaymentMethod>(paymentInfo.PaymentMethod, true, out var method))
                        throw new ValidationException($"Invalid payment method: {paymentInfo.PaymentMethod}");

                    if (paymentInfo.Amount <= 0)
                        throw new ValidationException("Payment amount must be greater than 0.");

                    await _paymentRepository.AddAsync(new Payment
                    {
                        Id = Guid.NewGuid(),
                        SaleId = sale.Id,
                        TenantId = tenantId,
                        Method = method,
                        Amount = paymentInfo.Amount,
                        TransactionRef = paymentInfo.Reference,
                        PaidAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    });

                    // ── Accumulate once only ──────────────────────────────────────────
                    if (method == PaymentMethod.Credit)
                        creditAmount += paymentInfo.Amount;
                    else
                        cashAndCardPaid += paymentInfo.Amount;

                    if (method == PaymentMethod.Cash)
                        cashAmount += paymentInfo.Amount;
                }
            }

            // ── Compute PaymentStatus ─────────────────────────────────────────────────
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

            // ── Update sale ───────────────────────────────────────────────────────────
            sale.CustomerId = request.CustomerId;
            sale.CashSessionId = cashSessionId;
            sale.SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate;
            sale.SubtotalAmount = subtotalAmount;
            sale.VatAmount = vatAmount;
            sale.TotalAmount = totalAmount;
            sale.PaidAmount = paidAmount;
            sale.ChangeAmount = request.ChangeAmount;
            sale.Status = SaleStatus.Completed;
            sale.PaymentStatus = paymentStatus;
            sale.Notes = request.Notes;
            sale.ModifiedAt = DateTime.UtcNow;

            _repository.Update(sale);

            // ── Persist sale lines ────────────────────────────────────────────────────
            var saleLines = lineResolutions.Select(r =>
            {
                var line = r.OriginalLine;

                if (r.StockQuantity <= 0)
                    throw new ValidationException($"Invalid stock quantity for product {r.StockProductId}");

                var unitProduct =
                    products.FirstOrDefault(p => p.Id == r.StockProductId)
                    ?? allTenantProductsForCatalogs.FirstOrDefault(p => p.Id == r.StockProductId);

                if (unitProduct == null)
                    throw new NotFoundException("Unit product", r.StockProductId);

                decimal unitCostPrice = r.IsPack
                    ? unitProduct.PurchasePrice / r.PackSize
                    : unitProduct.PurchasePrice;

                var lineGross = line.Quantity * line.UnitPrice;
                var lineDiscount = lineGross * (line.DiscountPercent / 100m);

                return new SaleLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SaleId = sale.Id,

                    ProductId = line.ProductId,
                    Quantity = line.Quantity,

                    UnitProductId = r.StockProductId,
                    UnitQuantity = r.StockQuantity,

                    UnitPrice = line.UnitPrice,
                    VatRate = line.VatRate,
                    DiscountPercent = line.DiscountPercent,
                    DiscountAmount = Math.Round(lineDiscount, 2, MidpointRounding.AwayFromZero),

                    UnitCostPrice = Math.Round(unitCostPrice, 4),

                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };
            }).ToList();

            await _saleLineRepository.AddRangeAsync(saleLines);

            // ── Cash drawer movement ──────────────────────────────────────────────────
            if (cashAmount > 0)
            {
                var last = await _cashMovementRepository.GetLastAsync(
            movement =>
                movement.CashSessionId ==
                    cashSessionId &&
                !movement.IsDeleted &&
                movement.TenantId ==
                    tenantId,
            movement =>
                movement.MovementDate);

                var before = last?.BalanceAfter ?? 0m;
                var after = before + cashAmount - sale.ChangeAmount;

                if (after < 0)
                    throw new ValidationException("Cash drawer cannot go negative.");

                await _cashMovementRepository.AddAsync(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CashSessionId = cashSessionId,
                    Type = CashMovementType.Sale,
                    Amount = cashAmount - sale.ChangeAmount,
                    BalanceBefore = before,
                    BalanceAfter = after,
                    ReferenceId = sale.Id,
                    ReferenceType = "Sale",
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // ── Customer credit transaction ───────────────────────────────────────────
            if (creditAmount >= 0.01m)
            {
                if (!sale.CustomerId.HasValue ||
                    sale.CustomerId.Value == Guid.Empty)
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
                    customer.TenantId != tenantId)
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
                        balanceBefore + creditAmount,
                        2,
                        MidpointRounding.AwayFromZero);

                if (!customer.HasUnlimitedCredit && balanceAfter > customer.CreditLimit)
                {
                    throw new ValidationException(
                        $"The customer credit limit would be exceeded. " +
                        $"Limit: {customer.CreditLimit:0.00}, " +
                        $"new balance: {balanceAfter:0.00}.");
                }

                customer.CurrentBalance =
                    balanceAfter;

                customer.ModifiedAt =
                    DateTime.UtcNow;

                _customerRepository.Update(
                    customer);

                await _customerTransactionRepository.AddAsync(
                    new CustomerTransaction
                    {
                        Id = Guid.NewGuid(),

                        TenantId = tenantId,

                        CustomerId = customer.Id,

                        SaleId = sale.Id,

                        Type = "Credit",

                        Amount = creditAmount,

                        BalanceBefore = balanceBefore,

                        BalanceAfter = balanceAfter,

                        TransactionDate = DateTime.UtcNow,

                        Description = $"Credit sale {sale.InvoiceNumber}",

                        CreatedAt = DateTime.UtcNow,

                        ModifiedAt = DateTime.UtcNow
                    });
            }

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(sale);
        }

        public async Task<SaleResult> UpdateCompletedAsync(Guid id, UpdateSaleRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            var sale = await _repository.Query()
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    !s.IsDeleted &&
                    s.TenantId == tenantId);

            if (sale == null)
                throw new NotFoundException("Sale", id);

            if (request.Lines == null || !request.Lines.Any())
                throw new ValidationException("Sale must contain at least one line.");

            // =========================
            // 1. REVERT STOCK
            // =========================
            var movements = await _stockMovementRepository.GetAsync(m =>
                m.ReferenceId == sale.Id &&
                !m.IsDeleted &&
                m.TenantId == tenantId);

            foreach (var m in movements)
            {
                var stock = await _stockRepository.GetSingleAsync(s =>
                    s.ProductId == m.ProductId &&
                    !s.IsDeleted &&
                    s.TenantId == tenantId);

                if (stock == null)
                    throw new NotFoundException("Stock", m.ProductId);

                stock.Quantity += Math.Abs(m.QuantityChange);
                stock.ModifiedAt = DateTime.UtcNow;

                _stockRepository.Update(stock);
                _stockMovementRepository.Delete(m);
            }

            // =========================
            // 2. DELETE SIDE EFFECTS
            // =========================
            var payments = await _paymentRepository.GetAsync(p => p.SaleId == sale.Id);
            foreach (var p in payments)
                _paymentRepository.Delete(p);

            var cashMovements = await _cashMovementRepository.GetAsync(c =>
                    c.ReferenceId == sale.Id &&
                    c.ReferenceType == "Sale");

            foreach (var c in cashMovements)
                _cashMovementRepository.Delete(c);

            var customerTx = await _customerTransactionRepository.GetAsync(c => c.SaleId == sale.Id);
            foreach (var c in customerTx)
                _customerTransactionRepository.Delete(c);

            var existingLines = await _saleLineRepository.GetAsync(l => l.SaleId == sale.Id);
            foreach (var l in existingLines)
                _saleLineRepository.Delete(l);

            // =========================
            // 3. REBUILD SALE (same as CreateComplete)
            // =========================

            var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();

            var products = await _productRepository.Query()
                .Include(p => p.CatalogProduct)
                .Where(p => productIds.Contains(p.Id) && p.TenantId == tenantId)
                .ToListAsync();

            var productMap = products.ToDictionary(p => p.Id);

            var catalogIds = products.Select(p => p.CatalogProductId).Distinct().ToList();

            var unitProducts = await _productRepository.Query()
                .Where(p => catalogIds.Contains(p.CatalogProductId) && p.TenantId == tenantId)
                .ToListAsync();

            var unitProductMap = unitProducts
                .GroupBy(p => p.CatalogProductId)
                .ToDictionary(g => g.Key, g => g.First());

            var lineResolutions = new List<(UpdateSaleLineRequest line, Guid stockProductId, decimal stockQty)>();

            foreach (var line in request.Lines)
            {
                if (!productMap.TryGetValue(line.ProductId, out var product))
                    throw new NotFoundException("Product", line.ProductId);

                var catalogId = product.CatalogProductId;

                if (_packService.IsPack(catalogId))
                {
                    var componentCatalogId = _packService.GetComponentCatalogId(catalogId);

                    if (!componentCatalogId.HasValue)
                        throw new ValidationException("Pack configuration invalid.");

                    if (!unitProductMap.TryGetValue(componentCatalogId.Value, out var unitProduct))
                        throw new NotFoundException("Unit product", componentCatalogId.Value);

                    var qty = _packService.GetUnitQuantity(catalogId, line.Quantity);

                    lineResolutions.Add((line, unitProduct.Id, qty));
                }
                else
                {
                    lineResolutions.Add((line, line.ProductId, line.Quantity));
                }
            }

            // =========================
            // STOCK VALIDATION
            // =========================
            var stockIds = lineResolutions.Select(x => x.stockProductId).Distinct().ToList();

            var stocks = await _stockRepository.GetAsync(s =>
                stockIds.Contains(s.ProductId) &&
                !s.IsDeleted &&
                s.TenantId == tenantId);

            var stockMap = stocks.ToDictionary(s => s.ProductId);

            foreach (var group in lineResolutions.GroupBy(x => x.stockProductId))
            {
                var required = group.Sum(x => x.stockQty);

                if (!stockMap.TryGetValue(group.Key, out var stock))
                    throw new NotFoundException("Stock", group.Key);

                if (stock.Quantity < required)
                    throw new ValidationException($"Insufficient stock for {group.Key}");
            }

            // =========================
            // APPLY STOCK
            // =========================
            var stockMovementsNew = new List<StockMovement>();

            foreach (var group in lineResolutions.GroupBy(x => x.stockProductId))
            {
                var stock = stockMap[group.Key];

                var qty = group.Sum(x => x.stockQty);

                var before = stock.Quantity;
                var after = before - qty;

                stock.Quantity = after;
                stock.ModifiedAt = DateTime.UtcNow;

                _stockRepository.Update(stock);

                stockMovementsNew.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = group.Key,
                    QuantityChange = -qty,
                    QuantityBefore = before,
                    QuantityAfter = after,
                    Type = StockMovementType.Sale,
                    ReferenceId = sale.Id,
                    ReferenceNumber = sale.InvoiceNumber,
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });
            }

            await _stockMovementRepository.AddRangeAsync(stockMovementsNew);

            // =========================
            // TOTALS
            // =========================
            decimal subtotal = 0m;
            decimal vat = 0m;
            decimal total = 0m;

            foreach (var r in lineResolutions)
            {
                var line = r.line;

                var gross = line.Quantity * line.UnitPrice;
                var discount = gross * (line.DiscountPercent / 100m);
                var net = gross - discount;

                var divisor = 1m + (line.VatRate / 100m);
                var ht = net / divisor;
                var v = net - ht;

                subtotal += Math.Round(ht, 2);
                vat += Math.Round(v, 2);
                total += Math.Round(net, 2);
            }

            // =========================
            // SAVE LINES
            // =========================
            var allResolvedProducts = products
                .Concat(unitProducts)
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var newLines = lineResolutions.Select(r =>
            {
                var l = r.line;

                if (r.stockQty <= 0)
                    throw new ValidationException($"Invalid stock quantity for product {r.stockProductId}");

                if (!allResolvedProducts.TryGetValue(r.stockProductId, out var stockProduct))
                    throw new NotFoundException("Product", r.stockProductId);

                var gross = l.Quantity * l.UnitPrice;
                var discount = gross * (l.DiscountPercent / 100m);

                return new SaleLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SaleId = sale.Id,

                    ProductId = l.ProductId,
                    Quantity = l.Quantity,

                    UnitProductId = r.stockProductId,
                    UnitQuantity = r.stockQty,

                    UnitPrice = l.UnitPrice,
                    VatRate = l.VatRate,
                    DiscountPercent = l.DiscountPercent,
                    DiscountAmount = Math.Round(discount, 2, MidpointRounding.AwayFromZero),

                    UnitCostPrice = Math.Round(stockProduct.PurchasePrice, 4),

                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };
            }).ToList();

            await _saleLineRepository.AddRangeAsync(newLines);

            // =========================
            // UPDATE SALE
            // =========================
            sale.SubtotalAmount = Math.Round(subtotal, 2);
            sale.VatAmount = Math.Round(vat, 2);
            sale.TotalAmount = Math.Round(total, 2);
            sale.CustomerId = request.CustomerId;
            sale.Notes = request.Notes;
            sale.Status = SaleStatus.Completed;
            sale.PaymentStatus = request.PaymentStatus;
            sale.PaidAmount = request.PaidAmount;
            sale.ChangeAmount = request.ChangeAmount;
            sale.ModifiedAt = DateTime.UtcNow;

            _repository.Update(sale);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(sale);
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
        public async Task<SaleResult> UpdateAsync(Guid id, UpdateSaleRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            var sale = await _repository.Query()
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    !s.IsDeleted &&
                    s.TenantId == tenantId);

            if (sale == null)
                throw new NotFoundException("Sale", id);

            if (sale.Status != SaleStatus.Pending)
                throw new ValidationException("Only pending sales can be updated with this method.");

            if (request.Lines == null || !request.Lines.Any())
                throw new ValidationException("Sale must contain at least one line.");

            // Delete old lines
            var existingLines = await _saleLineRepository.GetAsync(l => l.SaleId == id);
            foreach (var existingLine in existingLines)
                _saleLineRepository.Delete(existingLine);

            // Preload products
            var productIds = request.Lines
                .Select(l => l.ProductId)
                .Distinct()
                .ToList();

            var products = await _productRepository.Query()
                .Include(p => p.CatalogProduct)
                .Where(p => productIds.Contains(p.Id) && p.TenantId == tenantId && !p.IsDeleted)
                .ToListAsync();

            var productMap = products.ToDictionary(p => p.Id);

            // preload all tenant products for possible pack component resolution
            var catalogIds = products
                .Select(p => p.CatalogProductId)
                .Distinct()
                .ToList();

            var tenantProducts = await _productRepository.Query()
                .Where(p => catalogIds.Contains(p.CatalogProductId) && p.TenantId == tenantId && !p.IsDeleted)
                .ToListAsync();

            var unitProductMap = tenantProducts
                .GroupBy(p => p.CatalogProductId)
                .ToDictionary(g => g.Key, g => g.First());

            var newLines = new List<SaleLine>();

            decimal subtotalAmount = 0m;
            decimal vatAmount = 0m;
            decimal totalAmount = 0m;

            foreach (var line in request.Lines)
            {
                if (!productMap.TryGetValue(line.ProductId, out var product))
                    throw new NotFoundException("Product", line.ProductId);

                if (line.Quantity <= 0)
                    throw new ValidationException($"Quantity must be greater than 0 for product {line.ProductId}.");

                if (line.UnitPrice < 0)
                    throw new ValidationException($"Unit price cannot be negative for product {line.ProductId}.");

                if (line.DiscountPercent < 0 || line.DiscountPercent > 100)
                    throw new ValidationException($"Discount percent must be between 0 and 100 for product {line.ProductId}.");

                if (line.VatRate < 0 || line.VatRate > 100)
                    throw new ValidationException($"VAT rate must be between 0 and 100 for product {line.ProductId}.");

                var catalogId = product.CatalogProductId;

                Guid unitProductId;
                decimal unitQuantity;

                if (_packService.IsPack(catalogId))
                {
                    var componentCatalogId = _packService.GetComponentCatalogId(catalogId);

                    if (componentCatalogId == null || componentCatalogId == Guid.Empty)
                        throw new ValidationException($"Pack configuration is invalid for catalog {catalogId}.");

                    if (!unitProductMap.TryGetValue(componentCatalogId.Value, out var unitProduct))
                        throw new NotFoundException("Unit product", componentCatalogId.Value);

                    unitProductId = unitProduct.Id;
                    unitQuantity = _packService.GetUnitQuantity(catalogId, line.Quantity);
                }
                else
                {
                    unitProductId = line.ProductId;
                    unitQuantity = line.Quantity;
                }

                var lineGross = line.Quantity * line.UnitPrice;
                var lineDiscount = lineGross * (line.DiscountPercent / 100m);
                var lineNetTtc = lineGross - lineDiscount;

                var divisor = 1m + (line.VatRate / 100m);
                var lineHt = divisor <= 0 ? lineNetTtc : lineNetTtc / divisor;
                var lineVat = lineNetTtc - lineHt;

                subtotalAmount += Math.Round(lineHt, 2, MidpointRounding.AwayFromZero);
                vatAmount += Math.Round(lineVat, 2, MidpointRounding.AwayFromZero);
                totalAmount += Math.Round(lineNetTtc, 2, MidpointRounding.AwayFromZero);

                newLines.Add(new SaleLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SaleId = sale.Id,

                    ProductId = line.ProductId,
                    Quantity = line.Quantity,

                    UnitProductId = unitProductId,
                    UnitQuantity = unitQuantity,

                    UnitPrice = line.UnitPrice,
                    VatRate = line.VatRate,
                    DiscountPercent = line.DiscountPercent,
                    DiscountAmount = Math.Round(lineDiscount, 2, MidpointRounding.AwayFromZero),

                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });
            }

            subtotalAmount = Math.Round(subtotalAmount, 2, MidpointRounding.AwayFromZero);
            vatAmount = Math.Round(vatAmount, 2, MidpointRounding.AwayFromZero);
            totalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero);

            await _saleLineRepository.AddRangeAsync(newLines);

            sale.CustomerId = request.CustomerId;
            sale.SaleDate = request.SaleDate == default ? sale.SaleDate : request.SaleDate;
            sale.SubtotalAmount = subtotalAmount;
            sale.VatAmount = vatAmount;
            sale.TotalAmount = totalAmount;
            sale.Notes = request.Notes;
            sale.ModifiedAt = DateTime.UtcNow;

            // keep pending state for this update method
            sale.Status = SaleStatus.Pending;
            sale.PaymentStatus = PaymentStatus.Pending;
            sale.PaidAmount = 0m;
            sale.ChangeAmount = 0m;

            _repository.Update(sale);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(sale);
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

        public async Task<SaleResult> UpdatePendingAsync(Guid id, CreatePendingSaleRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            var sale = await _repository.Query()
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.Status == SaleStatus.Pending &&
                    !s.IsDeleted &&
                    s.TenantId == tenantId);

            if (sale == null)
                throw new NotFoundException("Pending Sale", id);

            // DELETE OLD LINES
            var existingLines = await _saleLineRepository.GetAsync(l => l.SaleId == sale.Id);
            foreach (var line in existingLines)
                _saleLineRepository.Delete(line);

            await _unitOfWork.SaveChangesAsync();

            // PRELOAD PRODUCTS
            var productIds = request.SaleLines.Select(l => l.ProductId).Distinct().ToList();

            var products = await _productRepository.Query()
                .Include(p => p.CatalogProduct)
                    .ThenInclude(c => c.PackComponents)
                .Where(p => productIds.Contains(p.Id) && p.TenantId == tenantId)
                .ToListAsync();

            var productMap = products.ToDictionary(p => p.Id);

            // AFTER (replace with this):
            var catalogIds = products
                .Select(p => p.CatalogProductId)
                .Distinct()
                .ToList();

            var componentCatalogIds = catalogIds
                .Where(id => _packService.IsPack(id))
                .Select(id => _packService.GetComponentCatalogId(id))
                .Where(id => id.HasValue && id != Guid.Empty)
                .Select(id => id!.Value)
                .ToList();

            var allTenantProductsForCatalogs = await _productRepository.Query()
                .Include(p => p.CatalogProduct)
                .Where(p => componentCatalogIds.Contains(p.CatalogProductId)
                         && p.TenantId == tenantId
                         && !p.IsDeleted)
                .ToListAsync();

            var unitProductMap = allTenantProductsForCatalogs
                .Where(p => p.CatalogProduct != null && !p.CatalogProduct.IsPack)
                .GroupBy(p => p.CatalogProductId)
                .ToDictionary(g => g.Key, g => g.First());

            // CREATE NEW LINES
            var newLines = new List<SaleLine>();

            foreach (var lineItem in request.SaleLines)
            {
                if (!productMap.TryGetValue(lineItem.ProductId, out var product))
                    throw new NotFoundException("Product", lineItem.ProductId);

                Guid unitProductId;

                if (product.CatalogProduct?.IsPack == true)
                {
                    var componentCatalogId = product.CatalogProduct
                        .PackComponents
                        .First()
                        .ComponentCatalogId;

                    if (!unitProductMap.TryGetValue(componentCatalogId, out var unitProduct))
                        throw new NotFoundException("Unit product", componentCatalogId);

                    unitProductId = unitProduct.Id;
                }
                else
                {
                    unitProductId = lineItem.ProductId;
                }

                var discount = lineItem.Quantity * lineItem.UnitPrice * (lineItem.DiscountPercent / 100m);

                newLines.Add(new SaleLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SaleId = sale.Id,

                    ProductId = lineItem.ProductId,
                    UnitProductId = unitProductId,

                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    VatRate = lineItem.VatRate,
                    DiscountPercent = lineItem.DiscountPercent,
                    DiscountAmount = Math.Round(discount, 2, MidpointRounding.AwayFromZero),

                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });
            }

            await _saleLineRepository.AddRangeAsync(newLines);

            // TOTALS
            var subtotalAmount = request.SaleLines.Sum(l => l.LineAmountExclVat);
            var vatAmount = request.SaleLines.Sum(l => l.LineVatAmount);
            var totalAmount = subtotalAmount + vatAmount;

            sale.CustomerId = request.CustomerId;
            sale.SaleDate = request.SaleDate ?? DateTime.UtcNow;
            sale.SubtotalAmount = subtotalAmount;
            sale.VatAmount = vatAmount;
            sale.TotalAmount = totalAmount;
            sale.ModifiedAt = DateTime.UtcNow;

            _repository.Update(sale);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(sale);
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
