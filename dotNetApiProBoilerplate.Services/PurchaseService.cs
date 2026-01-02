using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class PurchaseService
    {
        private readonly IRepository<Purchase> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDocumentNumberService _documentNumberService;

        public PurchaseService(
            IRepository<Purchase> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDocumentNumberService documentNumberService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _documentNumberService = documentNumberService;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<PurchaseResult> CreateAsync(CreatePurchaseRequest request)
        {
            if (request.TotalAmountInclVat <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmountInclVat", new[] { "TotalAmountInclVat must be > 0." } }
                });
            }

            if (request.TotalAmountExclVat <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmountExclVat", new[] { "TotalAmountExclVat must be > 0." } }
                });
            }

            var entity = _mapper.Map<Purchase>(request);

            entity.Id = Guid.NewGuid();
            entity.PurchaseNumber = await _documentNumberService.GenerateAsync("PURCHASE");

            entity.PurchaseDate = request.PurchaseDate == default
                ? DateTime.UtcNow
                : request.PurchaseDate;

            entity.TotalAmountExclVat = request.TotalAmountExclVat;
            entity.TotalVatAmount = request.TotalVatAmount;
            entity.TotalAmountInclVat = request.TotalAmountInclVat;

            entity.Status = PurchaseStatus.Received;

            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResult>(entity);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<PurchaseResult> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity is null || entity.IsDeleted)
                throw new NotFoundException("Purchase", id);

            return _mapper.Map<PurchaseResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<PurchaseResult>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();

            return _mapper.Map<List<PurchaseResult>>(
                entities.Where(x => !x.IsDeleted).ToList()
            );
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<PurchaseResult> UpdateAsync(Guid id, UpdatePurchaseRequest request)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity is null || entity.IsDeleted)
                throw new NotFoundException("Purchase", id);

            if (request.TotalAmountInclVat < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmountInclVat", new[] { "TotalAmountInclVat must be >= 0." } }
                });
            }

            if (request.TotalAmountExclVat < 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmountExclVat", new[] { "TotalAmountExclVat must be >= 0." } }
                });
            }

            _mapper.Map(request, entity);

            entity.TotalAmountExclVat = request.TotalAmountExclVat;
            entity.TotalVatAmount = request.TotalVatAmount;
            entity.TotalAmountInclVat = request.TotalAmountInclVat;

            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchaseResult>(entity);
        }

        // =========================
        // SOFT DELETE
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity is null || entity.IsDeleted)
                throw new NotFoundException("Purchase", id);

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY (pagination / filter / sort)
        // =========================
        public async Task<PagedResult<PurchaseResult>> QueryAsync(PurchaseQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var source = (await _repository.GetAllAsync())
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                source = source.Where(x =>
                    x.PurchaseNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    (x.SupplierInvoiceNumber != null &&
                     x.SupplierInvoiceNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase)));
            }

            if (query.Status.HasValue)
            {
                var status = (PurchaseStatus)query.Status.Value;
                source = source.Where(x => x.Status == status);
            }

            source = query.SortBy?.ToLower() switch
            {
                "purchasenumber" => query.Desc
                    ? source.OrderByDescending(x => x.PurchaseNumber)
                    : source.OrderBy(x => x.PurchaseNumber),

                "purchasedate" => query.Desc
                    ? source.OrderByDescending(x => x.PurchaseDate)
                    : source.OrderBy(x => x.PurchaseDate),

                _ => query.Desc
                    ? source.OrderByDescending(x => x.CreatedAt)
                    : source.OrderBy(x => x.CreatedAt)
            };

            var total = source.Count();

            var items = source
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

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
