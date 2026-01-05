using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Returns.Requests;
using Inventory.Dto.Returns.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Inventory.Domain.Enums;

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
        }

        // =========================
        // CREATE
        // =========================
        public async Task<ReturnResult> CreateAsync(CreateReturnRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();

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
            var tenantId = _tenantContext.GetTenantId();

            if (request.Lines == null || !request.Lines.Any())
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Lines", new[] { "At least one return line is required." } }
                });
            }

            var sale = await _saleRepository.GetByIdAsync(request.SaleId);
            if (sale == null || sale.TenantId != tenantId)
            {
                throw new NotFoundException("Sale", request.SaleId);
            }

            var entity = _mapper.Map<Return>(request);

            entity.Id = Guid.NewGuid();
            entity.TenantId = tenantId;
            entity.ReturnNumber = await _documentNumberService.GenerateAsync("RETURN");
            entity.ReturnDate = request.ReturnDate == default ? DateTime.UtcNow : request.ReturnDate;
            entity.TotalAmount = request.Lines.Sum(l => l.Quantity * l.UnitPrice);
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);

            foreach (var lineItem in request.Lines)
            {
                var line = new ReturnLine
                {
                    Id = Guid.NewGuid(),
                    ReturnId = entity.Id,
                    ProductId = lineItem.ProductId,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    VatRate = lineItem.VatRate,
                    Reason = lineItem.Reason,
                    RestockItem = lineItem.RestockItem
                };

                await _returnLineRepository.AddAsync(line);

                if (lineItem.RestockItem)
                {
                    var stock = await _stockRepository.GetSingleAsync(
                        s => s.ProductId == lineItem.ProductId && !s.IsDeleted);

                    decimal quantityBefore = stock?.Quantity ?? 0;

                    if (stock == null)
                    {
                        stock = new Stock
                        {
                            Id = Guid.NewGuid(),
                            ProductId = lineItem.ProductId,
                            Quantity = 0,
                            CreatedAt = DateTime.UtcNow,
                            ModifiedAt = DateTime.UtcNow
                        };
                        await _stockRepository.AddAsync(stock);
                    }

                    stock.Quantity += lineItem.Quantity;
                    stock.LastUpdated = DateTime.UtcNow;
                    stock.ModifiedAt = DateTime.UtcNow;

                    _stockRepository.Update(stock);

                    var movement = new StockMovement
                    {
                        Id = Guid.NewGuid(),
                        ProductId = lineItem.ProductId,
                        Type = StockMovementType.Return,
                        QuantityChange = lineItem.Quantity,
                        QuantityBefore = quantityBefore,
                        QuantityAfter = stock.Quantity,
                        ReferenceId = entity.Id,
                        ReferenceNumber = entity.ReturnNumber,
                        MovementDate = DateTime.UtcNow,
                        Notes = $"Return {entity.ReturnNumber}",
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    };

                    await _stockMovementRepository.AddAsync(movement);
                }
            }

            if (sale.CustomerId.HasValue)
            {
                var customer = await _customerRepository.GetByIdAsync(sale.CustomerId.Value);
                if (customer != null && !customer.IsDeleted)
                {
                    customer.CurrentBalance -= entity.TotalAmount;
                    customer.ModifiedAt = DateTime.UtcNow;
                    _customerRepository.Update(customer);

                    var trans = new CustomerTransaction
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer.Id,
                        Type = "Refund",
                        Amount = entity.TotalAmount,
                        BalanceBefore = customer.CurrentBalance + entity.TotalAmount,
                        BalanceAfter = customer.CurrentBalance,
                        TransactionDate = entity.ReturnDate,
                        SaleId = sale.Id,
                        Description = $"Return {entity.ReturnNumber} for Sale {sale.InvoiceNumber}",
                        TenantId = tenantId
                    };

                    await _customerTransactionRepository.AddAsync(trans);
                }
            }

            var today = entity.ReturnDate.Date;
            var summary = (await _salesSummaryRepository.GetAllAsync())
                .FirstOrDefault(x => x.Date == today && x.TenantId == tenantId && !x.IsDeleted);

            if (summary != null)
            {
                summary.TotalRevenue -= entity.TotalAmount;
                var totalVat = request.Lines.Sum(l => l.Quantity * l.UnitPrice * (l.VatRate / 100));
                summary.TotalVat -= totalVat;
                summary.CreditSales -= entity.TotalAmount;
                summary.ModifiedAt = DateTime.UtcNow;

                _salesSummaryRepository.Update(summary);
            }

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReturnResult>(entity);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<ReturnResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
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
            var tenantId = _tenantContext.GetTenantId();

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
            var tenantId = _tenantContext.GetTenantId();
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
            var tenantId = _tenantContext.GetTenantId();
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
            var tenantId = _tenantContext.GetTenantId();

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
