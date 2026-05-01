using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Returns.Requests;
using Inventory.Dto.Returns.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using RefundMethodDto = Inventory.Dto.Enums.RefundMethod;

namespace Inventory.Services
{
    public class ReturnService
    {
        private readonly IRepository<Return> _repository;
        private readonly IRepository<ReturnLine> _returnLineRepository;
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<CustomerTransaction> _customerTransactionRepository;
        private readonly IRepository<SalesSummaryDaily> _salesSummaryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDocumentNumberService _documentNumberService;
        private readonly ITenantContext _tenantContext;
        private readonly IRepository<CashMovement> _cashMovementRepository;
        private readonly ICashSessionService _cashSessionService;

        public ReturnService(
            IRepository<Return> repository,
            IRepository<ReturnLine> returnLineRepository,
            IRepository<StockMovement> stockMovementRepository,
            IRepository<Stock> stockRepository,
            IRepository<Sale> saleRepository,
            IRepository<Customer> customerRepository,
            IRepository<CustomerTransaction> customerTransactionRepository,
            IRepository<SalesSummaryDaily> salesSummaryRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDocumentNumberService documentNumberService,
            IRepository<CashMovement> cashMovementRepository,
            ICashSessionService cashSessionService,

            ITenantContext tenantContext)
        {
            _repository = repository;
            _returnLineRepository = returnLineRepository;
            _stockMovementRepository = stockMovementRepository;
            _stockRepository = stockRepository;
            _saleRepository = saleRepository;
            _customerRepository = customerRepository;
            _customerTransactionRepository = customerTransactionRepository;
            _salesSummaryRepository = salesSummaryRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _documentNumberService = documentNumberService;
            _tenantContext = tenantContext;
            _cashMovementRepository = cashMovementRepository;
            _cashSessionService = cashSessionService;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<ReturnResult> CreateAsync(CreateReturnRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            if (request.TotalAmount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmount", new[] { "TotalAmount must be > 0." } }
                });
            }

            var entity = _mapper.Map<Return>(request);

            entity.Id = Guid.NewGuid();
            entity.TenantId = tenantId;
            entity.ReturnNumber = await _documentNumberService.GenerateAsync("RETURN");
            entity.ReturnDate = request.ReturnDate == default ? DateTime.UtcNow : request.ReturnDate;
            entity.TotalAmount = request.TotalAmount;
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReturnResult>(entity);
        }

        // =========================
        // CREATE COMPLETE
        // =========================
        public async Task<ReturnResult> CreateCompleteAsync(CreateCompleteReturnRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var activeCashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

            // =========================
            // VALIDATION
            // =========================
            if (request.Lines == null || !request.Lines.Any())
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Lines", new[] { "At least one return line is required." } }
                });
            }

            if (request.RefundType == RefundMethodDto.Original)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "RefundType", new[] {
                        "Original refund method is not supported. Please select Cash, Card, Credit, or Exchange."
                    }}
                });
            }

            var sale = await _saleRepository.GetByIdAsync(request.SaleId);
            if (sale == null || sale.IsDeleted || sale.TenantId != tenantId)
                throw new NotFoundException("Sale", request.SaleId);

            if (request.RefundType == RefundMethodDto.Credit && !sale.CustomerId.HasValue)
                throw new ValidationException("Credit refund requires a customer.");

            if (request.RefundType == RefundMethodDto.Original)
                throw new ValidationException("Original refund method is not supported.");

            // =========================
            // CREATE RETURN HEADER
            // =========================
            var entity = new Return
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SaleId = sale.Id,
                ReturnNumber = await _documentNumberService.GenerateAsync("RETURN"),
                ReturnDate = request.ReturnDate == default ? DateTime.UtcNow : request.ReturnDate,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            entity.TotalAmount = request.Lines.Sum(l => l.Quantity * l.UnitPrice);

            await _repository.AddAsync(entity);

            // =========================
            // RETURN LINES + STOCK
            // =========================
            foreach (var lineItem in request.Lines)
            {
                var line = new ReturnLine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ReturnId = entity.Id,
                    ProductId = lineItem.ProductId,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    VatRate = lineItem.VatRate,
                    Reason = lineItem.Reason,
                    RestockItem = lineItem.RestockItem
                };

                await _returnLineRepository.AddAsync(line);

                if (!lineItem.RestockItem)
                    continue;

                var stock = await _stockRepository.GetSingleAsync(
                    s => s.ProductId == lineItem.ProductId &&
                         !s.IsDeleted &&
                         s.TenantId == tenantId);

                var quantityBefore = stock?.Quantity ?? 0;
                var quantityAfter = quantityBefore + lineItem.Quantity;

                if (stock == null)
                {
                    stock = new Stock
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ProductId = lineItem.ProductId,
                        Quantity = quantityAfter,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow
                    };
                    await _stockRepository.AddAsync(stock);
                }
                else
                {
                    stock.Quantity = quantityAfter;
                    stock.LastUpdated = DateTime.UtcNow;
                    stock.ModifiedAt = DateTime.UtcNow;
                    _stockRepository.Update(stock);
                }

                await _stockMovementRepository.AddAsync(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = lineItem.ProductId,
                    Type = StockMovementType.Return,
                    QuantityChange = lineItem.Quantity,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = quantityAfter,
                    ReferenceId = entity.Id,
                    ReferenceNumber = entity.ReturnNumber,
                    MovementDate = DateTime.UtcNow,
                    Notes = $"Return {entity.ReturnNumber}",
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });
            }

            // =========================
            // REFUND CALCULATION (CRITICAL)
            // =========================
            var refundAmount = entity.TotalAmount;

            decimal cashRefund = 0m;
            decimal creditRefund = 0m;

            switch (request.RefundType)
            {
                case RefundMethodDto.Cash:
                    cashRefund = refundAmount;
                    break;

                case RefundMethodDto.Credit:
                    creditRefund = refundAmount;
                    break;

                case RefundMethodDto.Card:
                    break;

                case RefundMethodDto.Exchange:
                    break;
            }

            // =========================
            // CUSTOMER REFUND (CREDIT PART)
            // =========================
            if (sale.CustomerId.HasValue && creditRefund > 0)
            {
                var customer = await _customerRepository.GetByIdAsync(sale.CustomerId.Value);

                if (customer != null && !customer.IsDeleted)
                {
                    var balanceBefore = customer.CurrentBalance;
                    var balanceAfter = balanceBefore - creditRefund;

                    customer.CurrentBalance = balanceAfter;
                    customer.ModifiedAt = DateTime.UtcNow;

                    _customerRepository.Update(customer);

                    await _customerTransactionRepository.AddAsync(new CustomerTransaction
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        CustomerId = customer.Id,
                        Type = "Refund",
                        Amount = creditRefund,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = balanceAfter,
                        TransactionDate = entity.ReturnDate,
                        SaleId = sale.Id,
                        Description = $"Return {entity.ReturnNumber} (credit part)"
                    });
                }
            }

            // =========================
            // SALES SUMMARY
            // =========================
            var today = entity.ReturnDate.Date;
            var summary = (await _salesSummaryRepository.GetAllAsync())
                .FirstOrDefault(x => x.Date == today &&
                                     x.TenantId == tenantId &&
                                     !x.IsDeleted);

            if (summary != null)
            {
                var totalVat = request.Lines.Sum(l => l.Quantity * l.UnitPrice * (l.VatRate / 100));

                if (request.RefundType != RefundMethodDto.Exchange)
                {
                    summary.TotalRevenue -= entity.TotalAmount;
                    summary.TotalVat -= totalVat;
                }

                summary.CashSales -= cashRefund;
                summary.CreditSales -= creditRefund;
                summary.ModifiedAt = DateTime.UtcNow;

                _salesSummaryRepository.Update(summary);
            }

            // =========================
            // CASH MOVEMENT — CASH PART ONLY
            // =========================
            if (cashRefund > 0)
            {
                var last = await _cashMovementRepository.GetLastAsync(
                    m => m.CashSessionId == activeCashSessionId &&
                         !m.IsDeleted &&
                         m.TenantId == tenantId,
                    m => m.MovementDate
                );

                var balanceBefore = last?.BalanceAfter ?? 0m;
                var balanceAfter = balanceBefore - cashRefund;

                if (balanceAfter < 0)
                    throw new ValidationException("Cash drawer cannot go negative.");

                await _cashMovementRepository.AddAsync(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CashSessionId = activeCashSessionId,
                    Type = CashMovementType.Refund,
                    Amount = cashRefund,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    ReferenceId = entity.Id,
                    ReferenceType = "Return",
                    Reason = $"Refund {entity.ReturnNumber} (cash part)",
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // =========================
            // COMMIT
            // =========================
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReturnResult>(entity);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<ReturnResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Return", id);

            return _mapper.Map<ReturnResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<ReturnResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var items = await _repository.GetAllAsync();

            return _mapper.Map<List<ReturnResult>>(
                items.Where(x => !x.IsDeleted && x.TenantId == tenantId).ToList()
            );
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<ReturnResult> UpdateAsync(Guid id, UpdateReturnRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Return", id);

            if (request.TotalAmount < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmount", new[] { "TotalAmount must be >= 0." } }
                });
            }

            _mapper.Map(request, entity);
            entity.TotalAmount = request.TotalAmount;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReturnResult>(entity);
        }

        // =========================
        // SOFT DELETE
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Return", id);

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<ReturnResult>> QueryAsync(ReturnQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var source = (await _repository.GetAllAsync())
                .Where(x => !x.IsDeleted && x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                source = source.Where(x =>
                    x.ReturnNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
            }

            source = query.SortBy?.ToLower() switch
            {
                "returnnumber" => query.Desc ? source.OrderByDescending(x => x.ReturnNumber) : source.OrderBy(x => x.ReturnNumber),
                "returndate" => query.Desc ? source.OrderByDescending(x => x.ReturnDate) : source.OrderBy(x => x.ReturnDate),
                _ => query.Desc ? source.OrderByDescending(x => x.CreatedAt) : source.OrderBy(x => x.CreatedAt)
            };

            var total = source.Count();
            var items = source
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<ReturnResult>
            {
                Items = _mapper.Map<List<ReturnResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
