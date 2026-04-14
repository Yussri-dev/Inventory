using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
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
            sale.SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate;
            sale.TotalAmount = request.TotalAmount;
            sale.CreatedAt = DateTime.UtcNow;
            sale.ModifiedAt = DateTime.UtcNow;

            sale.PaymentStatus = PaymentStatus.Paid;

            await _repository.AddAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(sale);
        }

        //public async Task<SaleResult> CreateCompleteAsync(CreateCompleteSaleRequest request)
        //{
        //    if (!_tenantContext.IsCashier && !_tenantContext.IsAdmin)
        //        throw new ForbiddenException("Only cashiers or admins can create sales.");

        //    var tenantId = _tenantContext.TenantId;
        //    var activeCashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

        //    var cashSession = await _cashSessionRepository.GetSingleAsync(cs =>
        //        cs.Id == activeCashSessionId &&
        //        !cs.IsDeleted &&
        //        cs.TenantId == tenantId);

        //    if (cashSession == null)
        //        throw new ValidationException("Active cash session not found for the current tenant.");

        //    Sale sale;
        //    var isExistingPending = request.PendingSaleId.HasValue;

        //    if (isExistingPending)
        //    {
        //        sale = await _repository.Query()
        //            .Include(s => s.Lines)
        //            .FirstOrDefaultAsync(s =>
        //                s.Id == request.PendingSaleId.Value &&
        //                s.Status == SaleStatus.Pending &&
        //                !s.IsDeleted &&
        //                s.TenantId == tenantId);

        //        if (sale == null)
        //            throw new NotFoundException("Pending Sale", request.PendingSaleId.Value);

        //        var existingLines = await _saleLineRepository.GetAsync(l => l.SaleId == sale.Id);
        //        foreach (var line in existingLines)
        //            _saleLineRepository.Delete(line);
        //    }
        //    else
        //    {
        //        sale = new Sale
        //        {
        //            Id = Guid.NewGuid(),
        //            TenantId = tenantId,
        //            InvoiceNumber = await _documentNumberService.GenerateAsync("191125"),
        //            CashSessionId = activeCashSessionId,
        //            SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate,
        //            CreatedAt = DateTime.UtcNow,
        //            ModifiedAt = DateTime.UtcNow,
        //            Status = SaleStatus.Pending,
        //            PaymentStatus = PaymentStatus.Pending
        //        };

        //        await _repository.AddAsync(sale);
        //        await _unitOfWork.SaveChangesAsync();
        //    }


        //    /*
        //    foreach (var line in request.Lines)
        //    {
        //        var stock = await _stockRepository.GetSingleAsync(
        //            s => s.ProductId == line.ProductId &&
        //                 !s.IsDeleted &&
        //                 s.TenantId == tenantId);

        //        if (stock == null)
        //            throw new NotFoundException("Stock", line.ProductId);

        //        if (stock.Quantity < line.Quantity)
        //            throw new ValidationException($"Insufficient stock for product {line.ProductId}.");
        //    }
        //    */

        //    // Fetch all stocks in one query
        //    var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        //    var stocks = await _stockRepository.GetAsync(
        //        s => productIds.Contains(s.ProductId) &&
        //             !s.IsDeleted &&
        //             s.TenantId == tenantId);
        //    var stockMap = stocks.ToDictionary(s => s.ProductId);

        //    // Validate all quantities first
        //    foreach (var line in request.Lines)
        //    {
        //        if (!stockMap.TryGetValue(line.ProductId, out var stock))
        //            throw new NotFoundException("Stock", line.ProductId);

        //        if (stock.Quantity < line.Quantity)
        //            throw new ValidationException($"Insufficient stock for product {line.ProductId}.");
        //    }

        //    // Apply deductions and create movements in memory
        //    var stockMovements = new List<StockMovement>();
        //    foreach (var lineItem in request.Lines)
        //    {
        //        var stock = stockMap[lineItem.ProductId];
        //        var quantityBefore = stock.Quantity;
        //        var quantityAfter = quantityBefore - lineItem.Quantity;

        //        stockMovements.Add(new StockMovement
        //        {
        //            Id = Guid.NewGuid(),
        //            TenantId = tenantId,
        //            ProductId = lineItem.ProductId,
        //            Type = StockMovementType.Sale,
        //            QuantityChange = -lineItem.Quantity,
        //            QuantityBefore = quantityBefore,
        //            QuantityAfter = quantityAfter,
        //            ReferenceId = sale.Id,
        //            ReferenceNumber = sale.InvoiceNumber,
        //            MovementDate = DateTime.UtcNow,
        //            Notes = $"Sale {sale.InvoiceNumber}",
        //            CreatedAt = DateTime.UtcNow,
        //            ModifiedAt = DateTime.UtcNow
        //        });

        //        stock.Quantity = quantityAfter;
        //        stock.LastUpdated = DateTime.UtcNow;
        //        stock.ModifiedAt = DateTime.UtcNow;
        //        _stockRepository.Update(stock);
        //    }

        //    await _stockMovementRepository.AddRangeAsync(stockMovements);

        //    decimal subtotalAmount = 0m;
        //    decimal vatAmount = 0m;
        //    decimal totalAmount = 0m;

        //    foreach (var line in request.Lines)
        //    {
        //        var lineGross = line.Quantity * line.UnitPrice;
        //        var lineDiscount = lineGross * (line.DiscountPercent / 100m);
        //        var lineNetTtc = lineGross - lineDiscount;

        //        var divisor = 1m + (line.VatRate / 100m);
        //        var lineHt = divisor <= 0 ? lineNetTtc : lineNetTtc / divisor;
        //        var lineVat = lineNetTtc - lineHt;

        //        subtotalAmount += Math.Round(lineHt, 2, MidpointRounding.AwayFromZero);
        //        vatAmount += Math.Round(lineVat, 2, MidpointRounding.AwayFromZero);
        //        totalAmount += Math.Round(lineNetTtc, 2, MidpointRounding.AwayFromZero);
        //    }

        //    subtotalAmount = Math.Round(subtotalAmount, 2, MidpointRounding.AwayFromZero);
        //    vatAmount = Math.Round(vatAmount, 2, MidpointRounding.AwayFromZero);
        //    totalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero);

        //    var paidAmount = request.Payments?.Sum(p => p.Amount) ?? 0m;

        //    if (paidAmount < 0)
        //        throw new ValidationException("Paid amount cannot be negative.");

        //    if (request.ChangeAmount < 0)
        //        throw new ValidationException("Change amount cannot be negative.");

        //    if (paidAmount < request.ChangeAmount)
        //        throw new ValidationException("Change amount cannot exceed paid amount.");

        //    if ((paidAmount - request.ChangeAmount) > totalAmount)
        //        throw new ValidationException("Invalid payment / change combination.");

        //    sale.CustomerId = request.CustomerId;
        //    sale.CashSessionId = activeCashSessionId;
        //    sale.SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate;
        //    sale.SubtotalAmount = subtotalAmount;
        //    sale.VatAmount = vatAmount;
        //    sale.TotalAmount = totalAmount;
        //    sale.PaidAmount = paidAmount;
        //    sale.ChangeAmount = request.ChangeAmount;
        //    sale.Status = SaleStatus.Completed;
        //    sale.PaymentStatus = (paidAmount - request.ChangeAmount) >= totalAmount
        //        ? PaymentStatus.Paid
        //        : (paidAmount > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Pending);
        //    sale.Notes = request.Notes;
        //    sale.ModifiedAt = DateTime.UtcNow;

        //    _repository.Update(sale);

        //    /*
        //    foreach (var lineItem in request.Lines)
        //    {
        //        var lineGross = lineItem.Quantity * lineItem.UnitPrice;
        //        var lineDiscount = lineGross * (lineItem.DiscountPercent / 100m);

        //        await _saleLineRepository.AddAsync(new SaleLine
        //        {
        //            Id = Guid.NewGuid(),
        //            TenantId = tenantId,
        //            SaleId = sale.Id,
        //            ProductId = lineItem.ProductId,
        //            Quantity = lineItem.Quantity,
        //            UnitPrice = lineItem.UnitPrice,
        //            VatRate = lineItem.VatRate,
        //            DiscountPercent = lineItem.DiscountPercent,
        //            DiscountAmount = Math.Round(lineDiscount, 2, MidpointRounding.AwayFromZero),
        //            CreatedAt = DateTime.UtcNow,
        //            ModifiedAt = DateTime.UtcNow
        //        });
        //    }
        //    */

        //    var saleLines = request.Lines.Select(lineItem =>
        //    {
        //        var lineGross = lineItem.Quantity * lineItem.UnitPrice;
        //        var lineDiscount = lineGross * (lineItem.DiscountPercent / 100m);
        //        return new SaleLine
        //        {
        //            Id = Guid.NewGuid(),
        //            TenantId = tenantId,
        //            SaleId = sale.Id,
        //            ProductId = lineItem.ProductId,
        //            Quantity = lineItem.Quantity,
        //            UnitPrice = lineItem.UnitPrice,
        //            VatRate = lineItem.VatRate,
        //            DiscountPercent = lineItem.DiscountPercent,
        //            DiscountAmount = Math.Round(lineDiscount, 2, MidpointRounding.AwayFromZero),
        //            CreatedAt = DateTime.UtcNow,
        //            ModifiedAt = DateTime.UtcNow
        //        };
        //    }).ToList();

        //    await _saleLineRepository.AddRangeAsync(saleLines);

        //    /*
        //    if (request.Payments != null && request.Payments.Any())
        //    {
        //        foreach (var paymentInfo in request.Payments)
        //        {
        //            if (!Enum.TryParse<PaymentMethod>(paymentInfo.PaymentMethod, true, out var method))
        //                throw new ValidationException($"Invalid payment method: {paymentInfo.PaymentMethod}");

        //            await _paymentRepository.AddAsync(new Payment
        //            {
        //                Id = Guid.NewGuid(),
        //                SaleId = sale.Id,
        //                TenantId = tenantId,
        //                Method = method,
        //                Amount = paymentInfo.Amount,
        //                TransactionRef = paymentInfo.Reference,
        //                PaidAt = DateTime.UtcNow,
        //                CreatedAt = DateTime.UtcNow,
        //                ModifiedAt = DateTime.UtcNow
        //            });

        //            if (method == PaymentMethod.Cash)
        //                cashAmount += paymentInfo.Amount;
        //        }
        //    }
        //    */
        //    decimal cashAmount = 0m;
        //    decimal cashAndCardPaid = 0m;

        //    if (request.Payments != null && request.Payments.Any())
        //    {
        //        foreach (var paymentInfo in request.Payments)
        //        {
        //            if (!Enum.TryParse<PaymentMethod>(paymentInfo.PaymentMethod, true, out var method))
        //                throw new ValidationException($"Invalid payment method: {paymentInfo.PaymentMethod}");

        //            // Enregistrement du paiement
        //            await _paymentRepository.AddAsync(new Payment
        //            {
        //                Id = Guid.NewGuid(),
        //                SaleId = sale.Id,
        //                TenantId = tenantId,
        //                Method = method,
        //                Amount = paymentInfo.Amount,
        //                TransactionRef = paymentInfo.Reference,
        //                PaidAt = DateTime.UtcNow,
        //                CreatedAt = DateTime.UtcNow,
        //                ModifiedAt = DateTime.UtcNow
        //            });

        //            // Agrégation fiable (enum uniquement)
        //            if (method != PaymentMethod.Credit)
        //            {
        //                cashAndCardPaid += paymentInfo.Amount;
        //            }

        //            if (method == PaymentMethod.Cash)
        //            {
        //                cashAmount += paymentInfo.Amount;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        cashAmount = 0m;
        //        cashAndCardPaid = 0m;
        //    }

        //    /*
        //    foreach (var lineItem in request.Lines)
        //    {
        //        var stock = await _stockRepository.GetSingleAsync(
        //            s => s.ProductId == lineItem.ProductId &&
        //                 !s.IsDeleted &&
        //                 s.TenantId == tenantId);

        //        if (stock == null)
        //            throw new NotFoundException("Stock", lineItem.ProductId);

        //        var quantityBefore = stock.Quantity;
        //        var quantityAfter = quantityBefore - lineItem.Quantity;

        //        await _stockMovementRepository.AddAsync(new StockMovement
        //        {
        //            Id = Guid.NewGuid(),
        //            TenantId = tenantId,
        //            ProductId = lineItem.ProductId,
        //            Type = StockMovementType.Sale,
        //            QuantityChange = -lineItem.Quantity,
        //            QuantityBefore = quantityBefore,
        //            QuantityAfter = quantityAfter,
        //            ReferenceId = sale.Id,
        //            ReferenceNumber = sale.InvoiceNumber,
        //            MovementDate = DateTime.UtcNow,
        //            Notes = $"Sale {sale.InvoiceNumber}",
        //            CreatedAt = DateTime.UtcNow,
        //            ModifiedAt = DateTime.UtcNow
        //        });

        //        stock.Quantity = quantityAfter;
        //        stock.LastUpdated = DateTime.UtcNow;
        //        stock.ModifiedAt = DateTime.UtcNow;
        //        _stockRepository.Update(stock);
        //    }
        //    */

        //    if (cashAmount > 0)
        //    {
        //        var last = await _cashMovementRepository.GetLastAsync(
        //            m => m.CashSessionId == activeCashSessionId &&
        //                 !m.IsDeleted &&
        //                 m.TenantId == tenantId,
        //            m => m.MovementDate);

        //        var before = last?.BalanceAfter ?? 0m;
        //        var after = before + cashAmount - sale.ChangeAmount;

        //        if (after < 0)
        //            throw new ValidationException("Cash drawer cannot go negative.");

        //        await _cashMovementRepository.AddAsync(new CashMovement
        //        {
        //            Id = Guid.NewGuid(),
        //            TenantId = tenantId,
        //            CashSessionId = activeCashSessionId,
        //            Type = CashMovementType.Sale,
        //            Amount = cashAmount - sale.ChangeAmount,
        //            BalanceBefore = before,
        //            BalanceAfter = after,
        //            SaleId = sale.Id,
        //            MovementDate = DateTime.UtcNow,
        //            CreatedAt = DateTime.UtcNow
        //        });
        //    }

        //    if (sale.CustomerId.HasValue)
        //    {
        //        //var cashAndCardPaid = request.Payments?
        //        //    .Where(p => !string.Equals(p.PaymentMethod, "Credit", StringComparison.OrdinalIgnoreCase))
        //        //    .Sum(p => p.Amount) ?? 0m;


        //        Console.WriteLine($"Total={sale.TotalAmount}, CashCard={cashAndCardPaid}, Change={sale.ChangeAmount}");
        //        Console.WriteLine($"CreditAmount would be={Math.Max(0, sale.TotalAmount - (cashAndCardPaid - sale.ChangeAmount))}");
        //        var creditAmount = Math.Max(0, sale.TotalAmount - (cashAndCardPaid - sale.ChangeAmount));

        //        if (creditAmount > 0)
        //        {
        //            var lastTx = await _customerTransactionRepository.GetLastAsync(
        //                t => t.CustomerId == sale.CustomerId.Value &&
        //                     !t.IsDeleted &&
        //                     t.TenantId == tenantId,
        //                t => t.TransactionDate);

        //            var balance = lastTx?.BalanceAfter ?? 0m;

        //            await _customerTransactionRepository.AddAsync(new CustomerTransaction
        //            {
        //                Id = Guid.NewGuid(),
        //                TenantId = tenantId,
        //                CustomerId = sale.CustomerId.Value,
        //                SaleId = sale.Id,
        //                Type = "Credit",
        //                Amount = creditAmount,
        //                BalanceBefore = balance,
        //                BalanceAfter = balance + creditAmount,
        //                TransactionDate = DateTime.UtcNow,
        //                CreatedAt = DateTime.UtcNow
        //            });
        //        }
        //    }

        //    await _unitOfWork.SaveChangesAsync();

        //    return _mapper.Map<SaleResult>(sale);
        //}

        public async Task<SaleResult> CreateCompleteAsync(CreateCompleteSaleRequest request)
        {
            if (!_tenantContext.IsCashier && !_tenantContext.IsAdmin)
                throw new ForbiddenException("Only cashiers or admins can create sales.");

            var tenantId = _tenantContext.TenantId;
            var activeCashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

            var cashSession = await _cashSessionRepository.GetSingleAsync(cs =>
                cs.Id == activeCashSessionId &&
                !cs.IsDeleted &&
                cs.TenantId == tenantId);

            if (cashSession == null)
                throw new ValidationException("Active cash session not found for the current tenant.");

            // ── Sale creation or pending resume ──────────────────────────────────────
            Sale sale;
            var isExistingPending = request.PendingSaleId.HasValue;

            if (isExistingPending)
            {
                sale = await _repository.Query()
                    .Include(s => s.Lines)
                    .FirstOrDefaultAsync(s =>
                        s.Id == request.PendingSaleId.Value &&
                        s.Status == SaleStatus.Pending &&
                        !s.IsDeleted &&
                        s.TenantId == tenantId);

                if (sale == null)
                    throw new NotFoundException("Pending Sale", request.PendingSaleId.Value);

                var existingLines = await _saleLineRepository.GetAsync(l => l.SaleId == sale.Id);
                foreach (var line in existingLines)
                    _saleLineRepository.Delete(line);
            }
            else
            {
                sale = new Sale
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    InvoiceNumber = await _documentNumberService.GenerateAsync("191125"),
                    CashSessionId = activeCashSessionId,
                    SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                    Status = SaleStatus.Pending,
                    PaymentStatus = PaymentStatus.Pending
                };

                await _repository.AddAsync(sale);
                await _unitOfWork.SaveChangesAsync();
            }

            // ── Résolution des lignes (pack → unité) ─────────────────────────────────
            var lineResolutions = new List<LineResolution>();

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

                    var unitProduct = await _productRepository.Query()
                        .FirstOrDefaultAsync(p =>
                            p.CatalogProductId == componentCatalogId &&
                            p.TenantId == tenantId);

                    if (unitProduct == null)
                        throw new NotFoundException(
                            "Unit product for pack", componentCatalogId ?? Guid.Empty);

                    lineResolutions.Add(new LineResolution
                    {
                        OriginalLine = line,
                        StockProductId = unitProduct.Id,
                        StockQuantity = unitQuantity,
                        IsPack = true,
                        PackSize = _packService.GetPackSize(catalogId)
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

            // ── Validation stock ─────────────────────────────────────────────────────
            var resolvedProductIds = lineResolutions
                .Select(r => r.StockProductId)
                .Distinct()
                .ToList();

            var stocks = await _stockRepository.GetAsync(
                s => resolvedProductIds.Contains(s.ProductId) &&
                     !s.IsDeleted &&
                     s.TenantId == tenantId);

            var stockMap = stocks.ToDictionary(s => s.ProductId);

            var requiredByProduct = lineResolutions
                .GroupBy(r => r.StockProductId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.StockQuantity));

            foreach (var (productId, requiredQty) in requiredByProduct)
            {
                if (!stockMap.TryGetValue(productId, out var stock))
                    throw new NotFoundException("Stock", productId);

                if (stock.Quantity < requiredQty)
                    throw new ValidationException(
                        $"Insufficient stock for product {productId}. " +
                        $"Required: {requiredQty}, Available: {stock.Quantity}");
            }

            // ── Déduction stock + mouvements ─────────────────────────────────────────
            var stockMovements = new List<StockMovement>();

            foreach (var resolution in lineResolutions)
            {
                var stock = stockMap[resolution.StockProductId];
                var quantityBefore = stock.Quantity;
                var quantityAfter = quantityBefore - resolution.StockQuantity;

                var notes = resolution.IsPack
                    ? $"Sale {sale.InvoiceNumber} (pack x{resolution.PackSize})"
                    : $"Sale {sale.InvoiceNumber}";

                stockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = resolution.StockProductId,
                    Type = StockMovementType.Sale,
                    QuantityChange = -resolution.StockQuantity,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = quantityAfter,
                    ReferenceId = sale.Id,
                    ReferenceNumber = sale.InvoiceNumber,
                    MovementDate = DateTime.UtcNow,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

                stock.Quantity = quantityAfter;
                stock.LastUpdated = DateTime.UtcNow;
                stock.ModifiedAt = DateTime.UtcNow;
                _stockRepository.Update(stock);
            }

            await _stockMovementRepository.AddRangeAsync(stockMovements);

            // ── Calcul des totaux ────────────────────────────────────────────────────
            decimal subtotalAmount = 0m;
            decimal vatAmount = 0m;
            decimal totalAmount = 0m;

            foreach (var line in request.Lines)
            {
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

            // ── Validation paiements ─────────────────────────────────────────────────
            var paidAmount = request.Payments?.Sum(p => p.Amount) ?? 0m;

            if (paidAmount < 0)
                throw new ValidationException("Paid amount cannot be negative.");

            if (request.ChangeAmount < 0)
                throw new ValidationException("Change amount cannot be negative.");

            if (paidAmount < request.ChangeAmount)
                throw new ValidationException("Change amount cannot exceed paid amount.");

            if ((paidAmount - request.ChangeAmount) > totalAmount)
                throw new ValidationException("Invalid payment / change combination.");

            // ── Mise à jour de la vente ──────────────────────────────────────────────
            sale.CustomerId = request.CustomerId;
            sale.CashSessionId = activeCashSessionId;
            sale.SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate;
            sale.SubtotalAmount = subtotalAmount;
            sale.VatAmount = vatAmount;
            sale.TotalAmount = totalAmount;
            sale.PaidAmount = paidAmount;
            sale.ChangeAmount = request.ChangeAmount;
            sale.Status = SaleStatus.Completed;
            sale.PaymentStatus = (paidAmount - request.ChangeAmount) >= totalAmount
                ? PaymentStatus.Paid
                : (paidAmount > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Pending);
            sale.Notes = request.Notes;
            sale.ModifiedAt = DateTime.UtcNow;

            _repository.Update(sale);

            // ── Sale lines ───────────────────────────────────────────────────────────
            var saleLines = request.Lines.Select(lineItem =>
            {
                var lineGross = lineItem.Quantity * lineItem.UnitPrice;
                var lineDiscount = lineGross * (lineItem.DiscountPercent / 100m);
                return new SaleLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SaleId = sale.Id,
                    ProductId = lineItem.ProductId,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    VatRate = lineItem.VatRate,
                    DiscountPercent = lineItem.DiscountPercent,
                    DiscountAmount = Math.Round(lineDiscount, 2, MidpointRounding.AwayFromZero),
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };
            }).ToList();

            await _saleLineRepository.AddRangeAsync(saleLines);

            // ── Paiements ────────────────────────────────────────────────────────────
            decimal cashAmount = 0m;
            decimal cashAndCardPaid = 0m;

            if (request.Payments != null && request.Payments.Any())
            {
                foreach (var paymentInfo in request.Payments)
                {
                    if (!Enum.TryParse<PaymentMethod>(paymentInfo.PaymentMethod, true, out var method))
                        throw new ValidationException($"Invalid payment method: {paymentInfo.PaymentMethod}");

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

                    if (method != PaymentMethod.Credit)
                        cashAndCardPaid += paymentInfo.Amount;

                    if (method == PaymentMethod.Cash)
                        cashAmount += paymentInfo.Amount;
                }
            }

            // ── Caisse ───────────────────────────────────────────────────────────────
            if (cashAmount > 0)
            {
                var last = await _cashMovementRepository.GetLastAsync(
                    m => m.CashSessionId == activeCashSessionId &&
                         !m.IsDeleted &&
                         m.TenantId == tenantId,
                    m => m.MovementDate);

                var before = last?.BalanceAfter ?? 0m;
                var after = before + cashAmount - sale.ChangeAmount;

                if (after < 0)
                    throw new ValidationException("Cash drawer cannot go negative.");

                await _cashMovementRepository.AddAsync(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CashSessionId = activeCashSessionId,
                    Type = CashMovementType.Sale,
                    Amount = cashAmount - sale.ChangeAmount,
                    BalanceBefore = before,
                    BalanceAfter = after,
                    SaleId = sale.Id,
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // ── Crédit client ─────────────────────────────────────────────────────────
            if (sale.CustomerId.HasValue)
            {
                var creditAmount = Math.Max(0, sale.TotalAmount - (cashAndCardPaid - sale.ChangeAmount));

                if (creditAmount > 0)
                {
                    var lastTx = await _customerTransactionRepository.GetLastAsync(
                        t => t.CustomerId == sale.CustomerId.Value &&
                             !t.IsDeleted &&
                             t.TenantId == tenantId,
                        t => t.TransactionDate);

                    var balance = lastTx?.BalanceAfter ?? 0m;

                    await _customerTransactionRepository.AddAsync(new CustomerTransaction
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        CustomerId = sale.CustomerId.Value,
                        SaleId = sale.Id,
                        Type = "Credit",
                        Amount = creditAmount,
                        BalanceBefore = balance,
                        BalanceAfter = balance + creditAmount,
                        TransactionDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

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

        //public async Task<List<SaleResult>> GetAllAsync()
        //{
        //    var tenantId = _tenantContext.TenantId;
        //    var sales = await _repository.GetAllAsync();

        //    return _mapper.Map<List<SaleResult>>(
        //        sales.Where(s => !s.IsDeleted && s.TenantId == tenantId).ToList()
        //    );
        //}
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
                var lineGross = lineItem.Quantity * lineItem.UnitPrice;
                var lineDiscount = lineGross * (lineItem.DiscountPercent / 100m);

                sale.Lines.Add(new SaleLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = lineItem.ProductId,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice, // TTC
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
            var sale = await _repository.GetByIdAsync(id);

            if (sale == null || sale.IsDeleted || sale.TenantId != tenantId)
                throw new NotFoundException("Sale", id);

            var originalInvoiceNumber = sale.InvoiceNumber;

            _mapper.Map(request, sale);

            sale.InvoiceNumber = originalInvoiceNumber;
            sale.TotalAmount = request.TotalAmount;
            sale.ModifiedAt = DateTime.UtcNow;

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

        /*
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

            var existingLines = await _saleLineRepository.GetAsync(l => l.SaleId == sale.Id);
            foreach (var line in existingLines)
                _saleLineRepository.Delete(line);

            await _unitOfWork.SaveChangesAsync();

            foreach (var lineItem in request.SaleLines)
            {
                await _saleLineRepository.AddAsync(new SaleLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SaleId = sale.Id,
                    ProductId = lineItem.ProductId,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    VatRate = lineItem.VatRate,
                    DiscountPercent = lineItem.DiscountPercent,
                    DiscountAmount = lineItem.Quantity * lineItem.UnitPrice * (lineItem.DiscountPercent / 100),
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });
            }

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
        */

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

            var existingLines = await _saleLineRepository.GetAsync(l => l.SaleId == sale.Id);
            foreach (var line in existingLines)
                _saleLineRepository.Delete(line);

            await _unitOfWork.SaveChangesAsync();

            var updatedLines = request.SaleLines.Select(lineItem => new SaleLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SaleId = sale.Id,
                ProductId = lineItem.ProductId,
                Quantity = lineItem.Quantity,
                UnitPrice = lineItem.UnitPrice,
                VatRate = lineItem.VatRate,
                DiscountPercent = lineItem.DiscountPercent,
                DiscountAmount = Math.Round(
                    lineItem.Quantity * lineItem.UnitPrice * (lineItem.DiscountPercent / 100m),
                    2, MidpointRounding.AwayFromZero),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }).ToList();

            await _saleLineRepository.AddRangeAsync(updatedLines);

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
        /*
        public async Task<PagedResult<SaleResult>> QueryAsync(SaleQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            var all = (await _repository.GetAllAsync())
                .Where(s => !s.IsDeleted
                        && s.TenantId == tenantId
                        && s.InvoiceNumber.Contains(query.Search.Trim()))
                .AsQueryable();

            var total = all.Count();

            var items = all
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<SaleResult>
            {
                Items = _mapper.Map<List<SaleResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        */
    }
}
