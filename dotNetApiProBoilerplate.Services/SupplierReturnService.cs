using AutoMapper;
using Inventory.Domain.Enums;
using Inventory.Domain.Models;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.SupplierReturns.Requests;
using Inventory.Dto.SupplierReturns.Results;
using Inventory.Dto.Suppliers.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;
using Inventory.Domain.Entities;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;

namespace Inventory.Services
{
    public class SupplierReturnService
    {
        private readonly IRepository<SupplierReturn> _repository;
        private readonly IRepository<SupplierReturnLine> _lineRepository;
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IRepository<Supplier> _supplierRepository;
        private readonly IRepository<SupplierTransaction> _supplierTransactionRepository;
        private readonly IDocumentNumberService _documentNumberService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public SupplierReturnService(
            IRepository<SupplierReturn> repository,
            IRepository<SupplierReturnLine> lineRepository,
            IRepository<StockMovement> stockMovementRepository,
            IRepository<Stock> stockRepository,
            IRepository<Supplier> supplierRepository,
            IRepository<SupplierTransaction> supplierTransactionRepository,
            IDocumentNumberService documentNumberService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _lineRepository = lineRepository;
            _stockMovementRepository = stockMovementRepository;
            _stockRepository = stockRepository;
            _supplierRepository = supplierRepository;
            _supplierTransactionRepository = supplierTransactionRepository;
            _documentNumberService = documentNumberService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // =========================
        // CREATE COMPLETE
        // =========================
        public async Task<SupplierReturnResult> CreateCompleteAsync(CreateCompleteSupplierReturnRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (request.Lines == null || !request.Lines.Any())
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Lines", new[] { "At least one line is required." } }
                });

            // Verify supplier belongs to tenant
            var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId);
            if (supplier == null || supplier.IsDeleted || supplier.TenantId != tenantId)
                throw new NotFoundException("Supplier", request.SupplierId);

            // 1. Create Header
            var entity = new SupplierReturn
            {
                Id = Guid.NewGuid(),
                SupplierId = request.SupplierId,
                TenantId = tenantId,
                ReturnNumber = request.ReturnNumber ?? await _documentNumberService.GenerateAsync("SUP_RET"),
                ReturnDate = request.ReturnDate,
                Reason = request.Reason ?? "Manual Return",
                Status = SupplierReturnStatus.Accepted,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(entity);

            decimal totalAmount = 0;

            // 2. Lines & Stock
            foreach (var item in request.Lines)
            {
                var line = new SupplierReturnLine
                {
                    Id = Guid.NewGuid(),
                    SupplierReturnId = entity.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Reason = item.Reason
                };
                await _lineRepository.AddAsync(line);

                totalAmount += item.Quantity * item.UnitPrice;

                var stock = await _stockRepository.GetSingleAsync(
                    s => s.ProductId == item.ProductId &&
                         !s.IsDeleted &&
                         s.TenantId == tenantId);

                decimal quantityBefore = 0;

                if (stock != null)
                {
                    quantityBefore = stock.Quantity;
                    stock.Quantity -= item.Quantity;
                    stock.ModifiedAt = DateTime.UtcNow;
                    _stockRepository.Update(stock);
                }
                else
                {
                    stock = new Stock
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ProductId = item.ProductId,
                        Quantity = -item.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    };
                    await _stockRepository.AddAsync(stock);
                }

                var sm = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = item.ProductId,
                    Type = StockMovementType.SupplierReturn,
                    QuantityChange = -item.Quantity,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = stock.Quantity,
                    ReferenceId = entity.Id,
                    ReferenceNumber = entity.ReturnNumber,
                    MovementDate = DateTime.UtcNow,
                    Notes = $"Supplier Return {entity.ReturnNumber}",
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };
                await _stockMovementRepository.AddAsync(sm);
            }

            // 3. Supplier Integration
            supplier.CurrentBalance -= totalAmount;
            supplier.ModifiedAt = DateTime.UtcNow;
            _supplierRepository.Update(supplier);

            var trans = new SupplierTransaction
            {
                Id = Guid.NewGuid(),
                SupplierId = supplier.Id,
                Type = SupplierTransactionType.Return,
                Amount = totalAmount,
                TransactionDate = DateTime.UtcNow,
                ReferenceNumber = entity.ReturnNumber,
                SupplierReturnId = entity.Id,
                Notes = $"Return {entity.ReturnNumber}",
                TenantId = tenantId
            };
            await _supplierTransactionRepository.AddAsync(trans);

            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<SupplierReturnResult>(entity);
        }

        // =========================
        // CREATE (Basic)
        // =========================
        public async Task<SupplierReturnResult> CreateAsync(CreateSupplierReturnRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();

            var exists = await _repository.ExistsAsync(r =>
                r.ReturnNumber == request.ReturnNumber &&
                r.TenantId == tenantId &&
                !r.IsDeleted);

            if (exists)
            {
                throw new ConflictException(
                    $"Supplier return '{request.ReturnNumber}' already exists.");
            }

            var entity = _mapper.Map<SupplierReturn>(request);

            entity.Id = Guid.NewGuid();
            entity.TenantId = tenantId;
            entity.Status = SupplierReturnStatus.Accepted;
            entity.ReturnDate = DateTime.UtcNow;
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SupplierReturnResult>(entity);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<SupplierReturnResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
            {
                throw new NotFoundException("SupplierReturn", id);
            }

            return _mapper.Map<SupplierReturnResult>(entity);
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<SupplierReturnResult> UpdateAsync(Guid id, UpdateSupplierReturnRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
            {
                throw new NotFoundException("SupplierReturn", id);
            }

            _mapper.Map(request, entity);
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SupplierReturnResult>(entity);
        }

        // =========================
        // DELETE (soft)
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
            {
                throw new NotFoundException("SupplierReturn", id);
            }

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<SupplierReturnResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var supplierReturns = await _repository.GetAllAsync();

            return _mapper.Map<List<SupplierReturnResult>>(
                supplierReturns.Where(s => !s.IsDeleted && s.TenantId == tenantId).ToList()
            );
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<SupplierReturnResult>> QueryAsync(SupplierReturnQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var returns = (await _repository.GetAllAsync())
                .Where(r => !r.IsDeleted && r.TenantId == tenantId)
                .AsQueryable();

            if (query.SupplierId.HasValue)
                returns = returns.Where(r => r.SupplierId == query.SupplierId.Value);

            if (query.Status.HasValue)
                returns = returns.Where(r => r.Status == (SupplierReturnStatus)query.Status.Value);

            if (query.FromDate.HasValue)
                returns = returns.Where(r => r.ReturnDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                returns = returns.Where(r => r.ReturnDate <= query.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                returns = returns.Where(r =>
                    r.ReturnNumber.Contains(query.Search) ||
                    r.Reason.Contains(query.Search));
            }

            returns = query.SortBy.ToLower() switch
            {
                "returndate" => query.Desc
                    ? returns.OrderByDescending(r => r.ReturnDate)
                    : returns.OrderBy(r => r.ReturnDate),

                "status" => query.Desc
                    ? returns.OrderByDescending(r => r.Status)
                    : returns.OrderBy(r => r.Status),

                _ => query.Desc
                    ? returns.OrderByDescending(r => r.CreatedAt)
                    : returns.OrderBy(r => r.CreatedAt)
            };

            var total = returns.Count();

            var items = returns
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<SupplierReturnResult>
            {
                Items = _mapper.Map<List<SupplierReturnResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
