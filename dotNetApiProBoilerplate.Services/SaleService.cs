using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;
using Inventory.Services.Abstractions;

namespace Inventory.Services
{
    public class SaleService
    {
        private readonly IRepository<Sale> _repository;
        private readonly IRepository<SaleLine> _saleLineRepository;
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDocumentNumberService _documentNumberService;

        public SaleService(
            IRepository<Sale> repository,
            IRepository<SaleLine> saleLineRepository,
            IRepository<StockMovement> stockMovementRepository,
            IRepository<Stock> stockRepository,
            IRepository<Payment> paymentRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDocumentNumberService documentNumberService)
        {
            _repository = repository;
            _saleLineRepository = saleLineRepository;
            _stockMovementRepository = stockMovementRepository;
            _stockRepository = stockRepository;
            _paymentRepository = paymentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _documentNumberService = documentNumberService;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<SaleResult> CreateAsync(CreateSaleRequest request)
        {
            if (request.TotalAmount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmount", new[] { "TotalAmount must be greater than 0." } }
                });
            }

            var sale = _mapper.Map<Sale>(request);

            sale.Id = Guid.NewGuid();
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
        // CREATE COMPLETE SALE (with Lines, StockMovements, and Payment)
        // =========================
        public async Task<SaleResult> CreateCompleteAsync(CreateCompleteSaleRequest request)
        {
            // 1. Validate stock availability for all products
            foreach (var line in request.Lines)
            {
                var stock = await _stockRepository.GetSingleAsync(s => 
                    s.ProductId == line.ProductId && !s.IsDeleted);
                    
                if (stock == null)
                {
                    throw new NotFoundException("Stock", line.ProductId);
                }
                    
                if (stock.Quantity < line.Quantity)
                {
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        { "Stock", new[] { $"Insufficient stock for product {line.ProductId}. Available: {stock.Quantity}, Required: {line.Quantity}" } }
                    });
                }
            }

            // 2. Create Sale header
            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = await _documentNumberService.GenerateAsync("SALE"),
                CustomerId = request.CustomerId,
                CashSessionId = request.CashSessionId,
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

            // 3. Create Sale Lines and Stock Movements
            foreach (var lineItem in request.Lines)
            {
                // Create SaleLine
                var saleLine = new SaleLine
                {
                    Id = Guid.NewGuid(),
                    SaleId = sale.Id,
                    ProductId = lineItem.ProductId,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    VatRate = lineItem.VatRate,
                    DiscountPercent = lineItem.DiscountPercent,
                    DiscountAmount = lineItem.Quantity * lineItem.UnitPrice * (lineItem.DiscountPercent / 100)
                };
                
                await _saleLineRepository.AddAsync(saleLine);
                
                // Get current stock for movement tracking
                var stock = await _stockRepository.GetSingleAsync(s => 
                    s.ProductId == lineItem.ProductId && !s.IsDeleted);
                
                var quantityBefore = stock.Quantity;
                var quantityAfter = quantityBefore - lineItem.Quantity;
                
                // Create Stock Movement
                var movement = new StockMovement
                {
                    Id = Guid.NewGuid(),
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
                };
                
                await _stockMovementRepository.AddAsync(movement);
                
                // Update Stock quantity
                stock.Quantity = quantityAfter;
                stock.LastUpdated = DateTime.UtcNow;
                stock.ModifiedAt = DateTime.UtcNow;
                _stockRepository.Update(stock);
            }

            // 4. Create Payment (if provided)
            if (request.Payment != null)
            {
                // Parse PaymentMethod from string to enum
                if (!Enum.TryParse<PaymentMethod>(request.Payment.PaymentMethod, true, out var paymentMethod))
                {
                    paymentMethod = PaymentMethod.Cash; // Default to Cash if parsing fails
                }
                
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    SaleId = sale.Id,
                    Method = paymentMethod,
                    Amount = request.Payment.Amount,
                    TransactionRef = request.Payment.Reference,
                    PaidAt = DateTime.UtcNow,
                    IsRefunded = false,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };
                
                await _paymentRepository.AddAsync(payment);
            }

            // 5. Save all changes atomically
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleResult>(sale);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<SaleResult> GetByIdAsync(Guid id)
        {
            var sale = await _repository.GetByIdAsync(id);

            if (sale == null || sale.IsDeleted)
            {
                throw new NotFoundException("Sale", id);
            }

            return _mapper.Map<SaleResult>(sale);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<SaleResult>> GetAllAsync()
        {
            var sales = await _repository.GetAllAsync();

            return _mapper.Map<List<SaleResult>>(
                sales.Where(s => !s.IsDeleted).ToList()
            );
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<SaleResult> UpdateAsync(Guid id, UpdateSaleRequest request)
        {
            var sale = await _repository.GetByIdAsync(id);

            if (sale == null || sale.IsDeleted)
            {
                throw new NotFoundException("Sale", id);
            }

            if (request.TotalAmount < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmount", new[] { "TotalAmount must be >= 0." } }
                });
            }

            // Never allow invoice number modification
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
            var sale = await _repository.GetByIdAsync(id);

            if (sale == null || sale.IsDeleted)
            {
                throw new NotFoundException("Sale", id);
            }

            sale.IsDeleted = true;
            sale.ModifiedAt = DateTime.UtcNow;

            _repository.Update(sale);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // PAGINATION + FILTERING + SORTING
        // =========================
        public async Task<PagedResult<SaleResult>> QueryAsync(SaleQuery query)
        {
            if (query.Page < 1)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Page", new[] { "Page must be >= 1." } }
                });
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                });
            }

            var all = (await _repository.GetAllAsync())
                .Where(s => !s.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                all = all.Where(s =>
                    s.InvoiceNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
            }

            all = query.SortBy?.ToLower() switch
            {
                "invoicenumber" => query.Desc
                    ? all.OrderByDescending(s => s.InvoiceNumber)
                    : all.OrderBy(s => s.InvoiceNumber),

                "saledate" => query.Desc
                    ? all.OrderByDescending(s => s.SaleDate)
                    : all.OrderBy(s => s.SaleDate),

                _ => query.Desc
                    ? all.OrderByDescending(s => s.CreatedAt)
                    : all.OrderBy(s => s.CreatedAt)
            };

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
