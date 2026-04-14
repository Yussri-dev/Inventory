using AutoMapper;
using AutoMapper.QueryableExtensions;
using Inventory.Domain.Barcodes;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;

namespace Inventory.Services
{
    public class ProductCatalogService
    {
        private readonly IRepository<ProductCatalog> _repository;
        private readonly IRepository<PackComponent> _packComponentRepository; // NOUVEAU
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public ProductCatalogService(
            IRepository<ProductCatalog> repository,
            IRepository<PackComponent> packComponentRepository, // NOUVEAU
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _packComponentRepository = packComponentRepository; // NOUVEAU
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // =========================
        // CREATE (SuperAdmin only)
        // =========================
        public async Task<ProductCatalogResult> CreateAsync(CreateProductCatalogRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Name", new[] { "ProductCatalog name must not be empty." } }
                });
            }

            if (string.IsNullOrWhiteSpace(request.Barcode))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Barcode", new[] { "ProductCatalog barcode must not be empty." } }
                });
            }

            // =========================
            // NORMALISATION CRITIQUE
            // =========================

            request.Barcode = new string(
                request.Barcode
                    .Trim()
                    .Where(char.IsLetterOrDigit)
                    .ToArray());

            request.Barcode = EanTools.Normalize(request.Barcode);

            // =========================
            // DÉTECTION
            // =========================

            var barcodeType = BarcodeDetector.Detect(request.Barcode);

            // =========================
            // VALIDATION
            // =========================

            if (!BarcodeValidator.IsValid(request.Barcode, barcodeType))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Barcode", new[] { "Invalid barcode format." } }
                });
            }

            // =========================
            // CONFLIT
            // =========================

            var exists = await _repository.ExistsAsync(c =>
                c.Barcode == request.Barcode &&
                !c.IsDeleted &&
                c.TenantId == tenantId);

            if (exists)
                throw new ConflictException("ProductCatalog with same barcode already exists.");

            // =========================
            // PERSISTENCE
            // =========================

            var entity = _mapper.Map<ProductCatalog>(request);

            entity.Id = Guid.NewGuid();
            entity.BarcodeType = barcodeType;
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedByUserId = userId;
            entity.TenantId = tenantId;
            entity.IsPack = request.IsPack; // NOUVEAU
            entity.UnitOfMeasure = request.UnitOfMeasure;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            // NOUVEAU — persister les composants pack
            if (request.IsPack && request.PackComponents.Any())
            {
                foreach (var comp in request.PackComponents)
                {
                    await _packComponentRepository.AddAsync(new PackComponent
                    {
                        Id = Guid.NewGuid(),
                        PackCatalaogId = entity.Id,
                        ComponentCatalogId = comp.ComponentCatalogId,
                        Quantity = comp.Quantity,
                        TenantId = tenantId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await _unitOfWork.SaveChangesAsync();
            }

            return _mapper.Map<ProductCatalogResult>(entity);


        }
        // =========================
        // GET BY ID (GLOBAL)
        // =========================
        public async Task<ProductCatalogResult> GetByIdAsync(Guid id)
        {
            var catalog = await _repository.GetByIdAsync(id);

            if (catalog == null || catalog.IsDeleted)
                throw new NotFoundException("ProductCatalog", id);

            return _mapper.Map<ProductCatalogResult>(catalog);
        }

        // =========================
        // GET ALL (GLOBAL)
        // =========================
        public async Task<List<ProductCatalogResult>> GetAllAsync()
        {
            var catalogs = await _repository.GetAllAsync();

            return _mapper.Map<List<ProductCatalogResult>>(
                catalogs.Where(c => !c.IsDeleted)
            );
        }

        // =========================
        // UPDATE (SuperAdmin only)
        // =========================
        public async Task<ProductCatalogResult> UpdateAsync(Guid id, UpdateProductCatalogRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var catalog = await _repository.Query()
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    !c.IsDeleted &&
                    c.TenantId == tenantId);

            if (catalog == null)
                throw new NotFoundException("ProductCatalog", id);

            // =========================
            // BARCODE CHANGE LOGIC
            // =========================

            if (!string.IsNullOrWhiteSpace(request.Barcode) &&
                request.Barcode != catalog.Barcode)
            {
                request.Barcode = request.Barcode.Trim();

                var type = BarcodeDetector.Detect(request.Barcode);

                if (!BarcodeValidator.IsValid(request.Barcode, type))
                {
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        { "Barcode", new[] { "Invalid barcode format." } }
                    });
                }

                var barcodeExists = await _repository.ExistsAsync(c =>
                    c.Barcode == request.Barcode &&
                    c.Id != id &&
                    !c.IsDeleted &&
                    c.TenantId == tenantId);

                if (barcodeExists)
                    throw new ConflictException("Another ProductCatalog uses this barcode.");

                catalog.BarcodeType = type;
            }

            // =========================
            // MAPPING SAFE FIELDS
            // =========================

            _mapper.Map(request, catalog);

            catalog.ModifiedAt = DateTime.UtcNow;
            catalog.ModifiedByUserId = userId;

            catalog.IsPack = request.IsPack;

            var existingComponents = await _packComponentRepository
                .Query()
                .Where(pc => pc.PackCatalaogId == id)
                .ToListAsync();

            foreach (var comp in existingComponents)
                _packComponentRepository.Delete(comp);

            if (request.IsPack && request.PackComponents.Any())
            {
                foreach (var comp in request.PackComponents)
                {
                    await _packComponentRepository.AddAsync(new PackComponent
                    {
                        Id = Guid.NewGuid(),
                        PackCatalaogId = id,
                        ComponentCatalogId = comp.ComponentCatalogId,
                        Quantity = comp.Quantity,
                        TenantId = tenantId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            _repository.Update(catalog);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ProductCatalogResult>(catalog);
        }
        // =========================
        // DELETE (SuperAdmin only)
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var catalog = await _repository.Query()
                .FirstOrDefaultAsync(c => c.Id == id
                    && !c.IsDeleted
                    && c.TenantId == tenantId);

            if (catalog == null || catalog.IsDeleted)
                throw new NotFoundException("ProductCatalog", id);

            catalog.IsDeleted = true;
            catalog.DeletedAt = DateTime.UtcNow;
            catalog.DeletedByUserId = _tenantContext.UserId;

            _repository.Update(catalog);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY (GLOBAL)
        // =========================
        /*
        public async Task<PagedResult<ProductCatalogResult>> QueryAsync(ProductCatalogQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var filtered = (await _repository.GetAllAsync())
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                filtered = filtered.Where(p =>
                    p.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    p.Barcode.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
            }

            filtered = query.SortBy?.ToLower() switch
            {
                "name" => query.Desc
                    ? filtered.OrderByDescending(p => p.Name)
                    : filtered.OrderBy(p => p.Name),

                "manufacturer" => query.Desc
                    ? filtered.OrderByDescending(p => p.Manufacturer)
                    : filtered.OrderBy(p => p.Manufacturer),

                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();

            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<ProductCatalogResult>
            {
                Items = _mapper.Map<List<ProductCatalogResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        */
        public async Task<PagedResult<ProductCatalogResult>> QueryAsync(ProductCatalogQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    {"Page", new[]{"Page must be greater than or equal to 1."} }
                });
            }

            if (query.PageSize < 1 || query.PageSize > 1000)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    {"PageSize", new[]{"PageSize must be between 1 and 100."} }
                });
            }

            var productCatalagQuery = _repository.Query()
                .Where(s => !s.IsDeleted
                        && s.TenantId == tenantId);
            //.AsNoTracking()
            //.AsQueryable();


            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                productCatalagQuery = productCatalagQuery.Where(p =>
                EF.Functions.ILike(p.Name, $"%{search}%") ||
                EF.Functions.ILike(p.Barcode, $"%{search}%") ||
                EF.Functions.ILike(p.Brand, $"%{search}%"));

            }

            productCatalagQuery = query.SortBy.ToLower() switch
            {
                "name" => query.Desc
                ? productCatalagQuery.OrderByDescending(p => p.Name)
                : productCatalagQuery.OrderBy(p => p.Name),
                "barcode" => query.Desc
                ? productCatalagQuery.OrderByDescending(p => p.Barcode)
                : productCatalagQuery.OrderBy(p => p.Barcode),

                _ => query.Desc
                ? productCatalagQuery.OrderByDescending(p => p.CreatedAt)
                : productCatalagQuery.OrderBy(p => p.CreatedAt),
            };



            var total = await productCatalagQuery.CountAsync();

            var items = await productCatalagQuery
                .Include(c => c.PackComponents)
                    .ThenInclude(pc => pc.ComponentCatalog)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ProjectTo<ProductCatalogResult>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new PagedResult<ProductCatalogResult>
            {
                Items = items,
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize,
            };

        }
    }
}
