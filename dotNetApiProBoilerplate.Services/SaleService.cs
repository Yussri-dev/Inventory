using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Domain.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;

namespace Inventory.Services
{
    public class SaleService
    {
        private readonly IRepository<Sale> _repository;
        private readonly IRepository<SaleLine> _saleLineRepository;
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<CustomerTransaction> _customerTransactionRepository;
        private readonly IRepository<CashMovement> _cashMovementRepository;
        //private readonly IRepository<CashSession> _cashSessionRepository;
        private readonly IRepository<LoyaltyCard> _loyaltyCardRepository;
        private readonly IRepository<LoyaltyTransaction> _loyaltyTransactionRepository;
        private readonly IRepository<SalesSummaryDaily> _salesSummaryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDocumentNumberService _documentNumberService;
        private readonly ICashSessionService _cashSessionService;
        private readonly ITenantContext _tenantContext;

        public SaleService(
            IRepository<Sale> repository,
            IRepository<SaleLine> saleLineRepository,
            IRepository<StockMovement> stockMovementRepository,
            IRepository<Stock> stockRepository,
            IRepository<Payment> paymentRepository,
            IRepository<Customer> customerRepository,
            IRepository<CustomerTransaction> customerTransactionRepository,
            IRepository<CashMovement> cashMovementRepository,
            IRepository<LoyaltyCard> loyaltyCardRepository,
            IRepository<LoyaltyTransaction> loyaltyTransactionRepository,
            IRepository<SalesSummaryDaily> salesSummaryRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICashSessionService cashSessionService,
            IDocumentNumberService documentNumberService,
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
            _cashSessionService = cashSessionService;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<SaleResult> CreateAsync(CreateSaleRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();

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
            sale.InvoiceNumber = await _documentNumberService.GenerateAsync("SALE");
            sale.SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate;
            sale.TotalAmount = request.TotalAmount;
            sale.CreatedAt = DateTime.UtcNow;
            sale.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(sale);
        }

        // =========================
        // CREATE COMPLETE SALE
        // =========================
        //public async Task<SaleResult> CreateCompleteAsync(CreateCompleteSaleRequest request)
        //{
        //    var tenantId = _tenantContext.GetTenantId();

        //    var activeCashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

        //    // Validation du stock
        //    foreach (var line in request.Lines)
        //    {
        //        var stock = await _stockRepository.GetSingleAsync(
        //            s => s.ProductId == line.ProductId && !s.IsDeleted && s.TenantId == tenantId);

        //        if (stock == null)
        //            throw new NotFoundException("Stock", line.ProductId);

        //        if (stock.Quantity < line.Quantity)
        //            throw new ValidationException(new Dictionary<string, string[]>
        //        {
        //            { "Stock", new[] { $"Insufficient stock for product {line.ProductId}." } }
        //        });
        //    }

        //    var sale = new Sale
        //    {
        //        Id = Guid.NewGuid(),
        //        TenantId = tenantId,
        //        InvoiceNumber = await _documentNumberService.GenerateAsync("SALE"),
        //        CustomerId = request.CustomerId,
        //        CashSessionId = activeCashSessionId, // ✅ Utiliser la session active
        //        SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate,
        //        SubtotalAmount = request.SubtotalAmount,
        //        DiscountAmount = request.DiscountAmount,
        //        VatAmount = request.VatAmount,
        //        TotalAmount = request.TotalAmount,
        //        PaidAmount = request.PaidAmount,
        //        ChangeAmount = request.ChangeAmount,
        //        Status = SaleStatus.Completed,
        //        PaymentStatus = request.Payment != null ? PaymentStatus.Paid : PaymentStatus.Pending,
        //        Notes = request.Notes,
        //        CreatedAt = DateTime.UtcNow,
        //        ModifiedAt = DateTime.UtcNow
        //    };

        //    await _repository.AddAsync(sale);

        //    foreach (var lineItem in request.Lines)
        //    {
        //        var saleLine = new SaleLine
        //        {
        //            Id = Guid.NewGuid(),
        //            SaleId = sale.Id,
        //            TenantId = tenantId,
        //            ProductId = lineItem.ProductId,
        //            Quantity = lineItem.Quantity,
        //            UnitPrice = lineItem.UnitPrice,
        //            VatRate = lineItem.VatRate,
        //            DiscountPercent = lineItem.DiscountPercent,
        //            DiscountAmount = lineItem.Quantity * lineItem.UnitPrice * (lineItem.DiscountPercent / 100)
        //        };

        //        await _saleLineRepository.AddAsync(saleLine);

        //        var stock = await _stockRepository.GetSingleAsync(
        //            s => s.ProductId == lineItem.ProductId && !s.IsDeleted && s.TenantId == tenantId);

        //        var quantityBefore = stock.Quantity;
        //        var quantityAfter = quantityBefore - lineItem.Quantity;

        //        var movement = new StockMovement
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
        //        };

        //        await _stockMovementRepository.AddAsync(movement);

        //        stock.Quantity = quantityAfter;
        //        stock.LastUpdated = DateTime.UtcNow;
        //        stock.ModifiedAt = DateTime.UtcNow;
        //        _stockRepository.Update(stock);
        //    }

        //    if (request.Payment != null)
        //    {
        //        Enum.TryParse<PaymentMethod>(request.Payment.PaymentMethod, true, out var method);

        //        var payment = new Payment
        //        {
        //            Id = Guid.NewGuid(),
        //            SaleId = sale.Id,
        //            TenantId = tenantId,
        //            Method = method,
        //            Amount = request.Payment.Amount,
        //            TransactionRef = request.Payment.Reference,
        //            PaidAt = DateTime.UtcNow,
        //            IsRefunded = false,
        //            CreatedAt = DateTime.UtcNow,
        //            ModifiedAt = DateTime.UtcNow
        //        };

        //        await _paymentRepository.AddAsync(payment);
        //    }

        //    // customer, cash, loyalty, reporting logic unchanged
        //    // tenant already propagated via sale.TenantId

        //    await _unitOfWork.SaveChangesAsync();

        //    return _mapper.Map<SaleResult>(sale);
        //}

        public async Task<SaleResult> CreateCompleteAsync(CreateCompleteSaleRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var activeCashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

            // =========================
            // STOCK VALIDATION
            // =========================
            foreach (var line in request.Lines)
            {
                var stock = await _stockRepository.GetSingleAsync(
                    s => s.ProductId == line.ProductId && !s.IsDeleted && s.TenantId == tenantId);

                if (stock == null)
                    throw new NotFoundException("Stock", line.ProductId);

                if (stock.Quantity < line.Quantity)
                    throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Stock", new[] { $"Insufficient stock for product {line.ProductId}." } }
            });
            }

            // =========================
            // CREATE SALE
            // =========================
            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InvoiceNumber = await _documentNumberService.GenerateAsync("SALE"),
                CustomerId = request.CustomerId,
                CashSessionId = activeCashSessionId,
                SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate,
                SubtotalAmount = request.SubtotalAmount,
                DiscountAmount = request.DiscountAmount,
                VatAmount = request.VatAmount,
                TotalAmount = request.TotalAmount,
                PaidAmount = request.PaidAmount,
                ChangeAmount = request.ChangeAmount,
                Status = SaleStatus.Completed,
                PaymentStatus = request.Payment != null ? PaymentStatus.Paid : PaymentStatus.Pending,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(sale);

            // =========================
            // SALE LINES + STOCK MOVEMENTS
            // =========================
            foreach (var lineItem in request.Lines)
            {
                var saleLine = new SaleLine
                {
                    Id = Guid.NewGuid(),
                    SaleId = sale.Id,
                    TenantId = tenantId,
                    ProductId = lineItem.ProductId,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    VatRate = lineItem.VatRate,
                    DiscountPercent = lineItem.DiscountPercent,
                    DiscountAmount = lineItem.Quantity * lineItem.UnitPrice * (lineItem.DiscountPercent / 100)
                };

                await _saleLineRepository.AddAsync(saleLine);

                var stock = await _stockRepository.GetSingleAsync(
                    s => s.ProductId == lineItem.ProductId && !s.IsDeleted && s.TenantId == tenantId);

                var quantityBefore = stock.Quantity;
                var quantityAfter = quantityBefore - lineItem.Quantity;

                await _stockMovementRepository.AddAsync(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = lineItem.ProductId,
                    Type = StockMovementType.Sale,
                    QuantityChange = -lineItem.Quantity,
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

            // =========================
            // PAYMENT
            // =========================
            decimal cashAmount = 0m;

            if (request.Payment != null)
            {
                Enum.TryParse<PaymentMethod>(request.Payment.PaymentMethod, true, out var method);

                await _paymentRepository.AddAsync(new Payment
                {
                    Id = Guid.NewGuid(),
                    SaleId = sale.Id,
                    TenantId = tenantId,
                    Method = method,
                    Amount = request.Payment.Amount,
                    TransactionRef = request.Payment.Reference,
                    PaidAt = DateTime.UtcNow,
                    IsRefunded = false,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

                if (method == PaymentMethod.Cash)
                    cashAmount = request.Payment.Amount;
            }

            // =========================
            // CASH MOVEMENTS (AUTHORITATIVE)
            // =========================

            // CASH IN — SALE
            if (cashAmount > 0)
            {
                var last = await _cashMovementRepository.GetLastAsync(
                    m => m.CashSessionId == activeCashSessionId &&
                         !m.IsDeleted &&
                         m.TenantId == tenantId,
                    m => m.MovementDate
                );

                var before = last?.BalanceAfter ?? 0m;
                var after = before + cashAmount;

                await _cashMovementRepository.AddAsync(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CashSessionId = activeCashSessionId,
                    Type = CashMovementType.Sale,
                    Amount = cashAmount,
                    BalanceBefore = before,
                    BalanceAfter = after,
                    SaleId = sale.Id,
                    Reason = $"Sale {sale.InvoiceNumber}",
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // CASH OUT — CHANGE
            if (sale.ChangeAmount > 0)
            {
                var last = await _cashMovementRepository.GetLastAsync(
                    m => m.CashSessionId == activeCashSessionId &&
                         !m.IsDeleted &&
                         m.TenantId == tenantId,
                    m => m.MovementDate
                );

                var before = last?.BalanceAfter ?? 0m;
                var after = before - sale.ChangeAmount;

                if (after < 0)
                    throw new ValidationException("Cash drawer cannot go negative.");

                await _cashMovementRepository.AddAsync(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CashSessionId = activeCashSessionId,
                    Type = CashMovementType.Refund,
                    Amount = sale.ChangeAmount,
                    BalanceBefore = before,
                    BalanceAfter = after,
                    SaleId = sale.Id,
                    Reason = $"Change for sale {sale.InvoiceNumber}",
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // =========================
            // COMMIT
            // =========================
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(sale);
        }


        // =========================
        // GET BY ID
        // =========================
        public async Task<SaleResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var sale = await _repository.GetByIdAsync(id);

            if (sale == null || sale.IsDeleted || sale.TenantId != tenantId)
                throw new NotFoundException("Sale", id);

            return _mapper.Map<SaleResult>(sale);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<SaleResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var sales = await _repository.GetAllAsync();

            return _mapper.Map<List<SaleResult>>(
                sales.Where(s => !s.IsDeleted && s.TenantId == tenantId).ToList()
            );
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<SaleResult> UpdateAsync(Guid id, UpdateSaleRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
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
            var tenantId = _tenantContext.GetTenantId();
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
        // QUERY
        // =========================
        public async Task<PagedResult<SaleResult>> QueryAsync(SaleQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            var all = (await _repository.GetAllAsync())
                .Where(s => !s.IsDeleted && s.TenantId == tenantId)
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
    }
}
