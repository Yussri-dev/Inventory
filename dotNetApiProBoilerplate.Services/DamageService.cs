using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Damages.Requests;
using Inventory.Dto.Damages.Results;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class DamageService
    {
        private readonly IRepository<Damage> _repository;
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDocumentNumberService _documentNumberService;
        private readonly ITenantContext _tenantContext;

        public DamageService(
            IRepository<Damage> repository,
            IRepository<StockMovement> stockMovementRepository,
            IRepository<Stock> stockRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDocumentNumberService documentNumberService,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _stockMovementRepository = stockMovementRepository;
            _stockRepository = stockRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _documentNumberService = documentNumberService;
            _tenantContext = tenantContext;
        }

        // CREATE
        public async Task<DamageResult> CreateAsync(CreateDamageRequest request)
        {
            if (request.Quantity <= 0)
                throw new ValidationException("Quantity must be > 0");

            var entity = _mapper.Map<Damage>(request);

            entity.Id = Guid.NewGuid();
            entity.TenantId = _tenantContext.TenantId;
            entity.CreatedByUserId = _tenantContext.UserId;
            entity.DamageNumber = await _documentNumberService.GenerateAsync("41370");
            entity.DamageDate = DateTime.UtcNow;
            entity.Status = DamageStatus.Draft;
            entity.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DamageResult>(entity);
        }


        // CREATE COMPLETE (Auto-approve & Stock Update)
        //public async Task<DamageResult> CreateCompleteAsync(CreateCompleteDamageRequest request)
        //{
        //    var tenantId = _tenantContext.TenantId;
        //    var userId = _tenantContext.UserId;

        //    if (request.Quantity <= 0)
        //    {
        //        throw new ValidationException(new Dictionary<string, string[]>
        //        {
        //            { "Quantity", new[] { "Quantity must be greater than 0." } }
        //        });
        //    }

        //    var entity = _mapper.Map<Damage>(request);

        //    entity.Id = Guid.NewGuid();
        //    entity.TenantId = tenantId;
        //    entity.CreatedByUserId = userId;
        //    entity.DamageNumber = await _documentNumberService.GenerateAsync("DAMAGE");
        //    entity.DamageDate = DateTime.UtcNow;
        //    //entity.IsApproved = true;
        //    //entity.ApprovedAt = DateTime.UtcNow;
        //    //entity.ApprovedByUserId = userId;
        //    entity.CreatedAt = DateTime.UtcNow;

        //    await _repository.AddAsync(entity);

        //    // Update Stock
        //    var stock = await _stockRepository.GetSingleAsync(s =>
        //        s.ProductId == request.ProductId &&
        //        s.TenantId == tenantId &&
        //        !s.IsDeleted);

        //    decimal quantityBefore = 0;

        //    if (stock == null)
        //    {
        //        stock = new Stock
        //        {
        //            Id = Guid.NewGuid(),
        //            ProductId = request.ProductId,
        //            TenantId = tenantId,
        //            CreatedByUserId = userId,
        //            Quantity = 0,
        //            ReservedQuantity = 0,
        //            CreatedAt = DateTime.UtcNow
        //        };
        //        await _stockRepository.AddAsync(stock);
        //    }
        //    if (stock.Quantity < request.Quantity)
        //    {
        //        throw new ValidationException(new Dictionary<string, string[]>
        //        {
        //            { "Quantity", new[] { "Damage quantity exceeds available stock." } }
        //        });
        //    }


        //    quantityBefore = stock.Quantity;
        //    stock.Quantity -= request.Quantity;
        //    stock.LastUpdated = DateTime.UtcNow;
        //    stock.ModifiedAt = DateTime.UtcNow;
        //    stock.ModifiedByUserId = userId;

        //    if (stock.Id != Guid.Empty)
        //        _stockRepository.Update(stock);

        //    // Create Stock Movement
        //    var movement = new StockMovement
        //    {
        //        Id = Guid.NewGuid(),
        //        ProductId = request.ProductId,
        //        TenantId = tenantId,
        //        CreatedByUserId = userId,
        //        Type = StockMovementType.Damage,
        //        QuantityChange = -request.Quantity,
        //        QuantityBefore = quantityBefore,
        //        QuantityAfter = stock.Quantity,
        //        ReferenceId = entity.Id,
        //        ReferenceNumber = entity.DamageNumber,
        //        MovementDate = DateTime.UtcNow,
        //        Notes = $"Damage {entity.DamageNumber}: {entity.Reason}",
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    await _stockMovementRepository.AddAsync(movement);
        //    await _unitOfWork.SaveChangesAsync();

        //    return _mapper.Map<DamageResult>(entity);
        //}

        // GET BY ID
        public async Task<DamageResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("Damage", id);

            return _mapper.Map<DamageResult>(entity);
        }

        // GET ALL
        public async Task<List<DamageResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var damages = (await _repository.GetAsync(
                 d => !d.IsDeleted && d.TenantId == tenantId,
                 d => d.Product
             ))
             .OrderByDescending(x => x.CreatedAt)
             .ToList();
            return _mapper.Map<List<DamageResult>>(damages);
        }


        // UPDATE
        public async Task<DamageResult> UpdateAsync(Guid id, UpdateDamageRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var entity = (await _repository.GetAsync(d => d.Id == id && !d.IsDeleted && d.TenantId == tenantId, d => d.Product)).FirstOrDefault();

            if (entity == null)
                throw new NotFoundException("Damage", id);

            if (entity.Status == DamageStatus.Validated)
                throw new ConflictException("Validated damages cannot be modified.");

            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedByUserId = userId;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DamageResult>(entity);
        }

        // APPROVE
        //public async Task<DamageResult> ApproveAsync(Guid id)
        //{
        //    var tenantId = _tenantContext.TenantId;
        //    var userId = _tenantContext.UserId;

        //    var entity = (await _repository.GetAsync(
        //        d => d.Id == id && !d.IsDeleted && d.TenantId == tenantId,
        //        d => d.Product
        //    )).FirstOrDefault();

        //    if (entity == null)
        //        throw new NotFoundException("Damage", id);

        //    //if (entity.IsApproved)
        //    //    throw new ConflictException("Damage is already approved.");

        //    //entity.IsApproved = true;
        //    //entity.ApprovedAt = DateTime.UtcNow;
        //    //entity.ApprovedByUserId = userId;
        //    entity.ModifiedAt = DateTime.UtcNow;
        //    entity.ModifiedByUserId = userId;

        //    _repository.Update(entity);
        //    await _unitOfWork.SaveChangesAsync();

        //    return _mapper.Map<DamageResult>(entity);
        //}

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var entity = (await _repository.GetAsync(
                d => d.Id == id && !d.IsDeleted && d.TenantId == tenantId,
                d => d.Product
            )).FirstOrDefault();

            if (entity == null)
                throw new NotFoundException("Damage", id);

            if (entity.Status == DamageStatus.Validated)
                throw new ConflictException("Validated damages cannot be deleted.");

            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedByUserId = userId;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedByUserId = userId;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }


        public async Task ValidateAllAsync()
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var drafts = await _repository.GetAsync(
                d => d.TenantId == tenantId &&
                     d.Status == DamageStatus.Draft &&
                     !d.IsDeleted,
                d => d.Product
            );

            if (!drafts.Any())
                throw new ValidationException("No draft damages to validate.");

            foreach (var damage in drafts)
            {
                var stock = await _stockRepository.GetSingleAsync(s =>
                    s.ProductId == damage.ProductId &&
                    s.TenantId == tenantId &&
                    !s.IsDeleted
                );

                if (stock == null)
                    throw new ValidationException(
                        $"No stock record for product '{damage.Product.Name}'."
                    );

                if (stock.Quantity < damage.Quantity)
                    throw new ValidationException(
                        $"Insufficient stock for product '{damage.Product.Name}'."
                    );

                var quantityBefore = stock.Quantity;

                stock.Quantity -= damage.Quantity;
                stock.ModifiedAt = DateTime.UtcNow;
                stock.ModifiedByUserId = userId;

                _stockRepository.Update(stock);

                await _stockMovementRepository.AddAsync(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = damage.ProductId,
                    TenantId = tenantId,
                    CreatedByUserId = userId,
                    Type = StockMovementType.Damage,
                    QuantityChange = -damage.Quantity,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = stock.Quantity,
                    ReferenceId = damage.Id,
                    ReferenceNumber = damage.DamageNumber,
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });

                damage.Status = DamageStatus.Validated;
                damage.ValidatedAt = DateTime.UtcNow;
                damage.ValidatedByUserId = userId;
                damage.ModifiedAt = DateTime.UtcNow;
                damage.ModifiedByUserId = userId;

                _repository.Update(damage);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // QUERY
        public async Task<PagedResult<DamageResult>> QueryAsync(DamageQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            var source = (await _repository.GetAsync(
                d => !d.IsDeleted && d.TenantId == tenantId,
                d => d.Product
            )).AsQueryable();

            if (query.ProductId.HasValue)
                source = source.Where(d => d.ProductId == query.ProductId.Value);

            //if (query.IsApproved.HasValue)
            //    source = source.Where(d => d.IsApproved == query.IsApproved.Value);

            if (!string.IsNullOrWhiteSpace(query.Category))
                source = source.Where(d => d.Category == query.Category);

            if (query.FromDate.HasValue)
                source = source.Where(d => d.DamageDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                source = source.Where(d => d.DamageDate <= query.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                source = source.Where(d =>
                    d.DamageNumber.Contains(query.Search) ||
                    d.Reason.Contains(query.Search));
            }

            source = query.SortBy?.ToLower() switch
            {
                "damagedate" => query.Desc ? source.OrderByDescending(d => d.DamageDate) : source.OrderBy(d => d.DamageDate),
                "quantity" => query.Desc ? source.OrderByDescending(d => d.Quantity) : source.OrderBy(d => d.Quantity),
                _ => query.Desc ? source.OrderByDescending(d => d.CreatedAt) : source.OrderBy(d => d.CreatedAt)
            };

            var total = source.Count();

            var items = source
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<DamageResult>
            {
                Items = _mapper.Map<List<DamageResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

    }
}