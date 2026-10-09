using AutoMapper;
using AutoMapper.QueryableExtensions;
using Inventory.Domain.Barcodes;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class ProductCatalogService
    {
        private readonly IRepository<ProductCatalog> _repository;
        private readonly IRepository<PackComponent> _packComponentRepository; // NOUVEAU
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;
        private readonly IProductProvisioningService _productProvisioningService;

        public ProductCatalogService(
            IRepository<ProductCatalog> repository,
            IRepository<PackComponent> packComponentRepository,
            IProductProvisioningService productProvisioningService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _packComponentRepository = packComponentRepository;
            _productProvisioningService = productProvisioningService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // =========================
        // CREATE (SuperAdmin only)
        // =========================
        public async Task<ProductCatalogResult> CreateAsync(CreateProductCatalogRequest request, Guid? excludedTenantId = null)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            // =========================
            // VALIDATION
            // =========================

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Name", new[] { "ProductCatalog name must not be empty." } }
        });
            }

            if (string.IsNullOrWhiteSpace(request.InternalCode))
            {
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "InternalCode", new[] { "InternalCode is required." } }
        });
            }

            if (request.SellingMode == SellingMode.Weight && request.UnitOfMeasure == "pcs")
                throw new ValidationException("Weight products cannot use pcs");

            if (request.SellingMode == SellingMode.Unit && request.UnitOfMeasure != "pcs")
                throw new ValidationException("Unit products must use pcs");

            request.InternalCode = request.InternalCode.Trim();

            var internalCodeExists = await _repository.ExistsAsync(c =>
                c.InternalCode == request.InternalCode &&
                !c.IsDeleted);

            if (internalCodeExists)
                throw new ConflictException("ProductCatalog with same InternalCode already exists.");

            // =========================
            // PACK VALIDATION (FIXED)
            // =========================

            if (request.IsPack == true &&
                 request.PackComponents != null &&
                 request.PackComponents.Any(x => x.Quantity <= 0))
            {
                throw new ValidationException("Pack component quantity must be > 0");
            }

            // =========================
            // BARCODE (OPTIONAL)
            // =========================

            BarcodeType? barcodeType = null;

            if (!string.IsNullOrWhiteSpace(request.Barcode))
            {
                request.Barcode = new string(
                    request.Barcode
                        .Trim()
                        .Where(char.IsLetterOrDigit)
                        .ToArray());

                request.Barcode = EanTools.Normalize(request.Barcode);

                var detectedType = BarcodeDetector.Detect(request.Barcode);

                if (!BarcodeValidator.IsValid(request.Barcode, detectedType))
                {
                    throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Barcode", new[] { "Invalid barcode format." } }
            });
                }

                var exists = await _repository.ExistsAsync(c =>
                    c.Barcode == request.Barcode &&
                    !c.IsDeleted);

                if (exists)
                    throw new ConflictException("ProductCatalog with same barcode already exists.");

                barcodeType = detectedType;
            }
            else
            {
                request.Barcode = null;
            }

            // =========================
            // PERSISTENCE
            // =========================

            var entity = _mapper.Map<ProductCatalog>(request);

            entity.Id = Guid.NewGuid();
            entity.BarcodeType = barcodeType;
            entity.InternalCode = request.InternalCode;
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedByUserId = userId;
            //entity.TenantId = tenantId;
            entity.IsPack = request.IsPack;
            entity.UnitOfMeasure = request.UnitOfMeasure;
            entity.SellingMode = request.SellingMode;

            await _repository.AddAsync(entity);

            if (request.IsPack && request.PackComponents != null && request.PackComponents.Any())
            {
                foreach (var comp in request.PackComponents)
                {
                    await _packComponentRepository.AddAsync(new PackComponent
                    {
                        Id = Guid.NewGuid(),
                        PackCatalaogId = entity.Id,
                        ComponentCatalogId = comp.ComponentCatalogId,
                        Quantity = comp.Quantity,
                        //TenantId = tenantId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();

            await _productProvisioningService
                .ProvisionCatalogProductToAllTenantsAsync(
                    entity.Id,
                    userId,
                    excludedTenantId);

            return _mapper.Map<ProductCatalogResult>(entity);
        }
        // =========================
        // GET BY ID (GLOBAL)
        // =========================
        public async Task<ProductCatalogResult> GetByIdAsync(Guid id)
        {
            var catalog = await _repository.Query()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (catalog == null)
                throw new NotFoundException("ProductCatalog", id);

            return _mapper.Map<ProductCatalogResult>(catalog);
        }

        // =========================
        // GET ALL (GLOBAL)
        // =========================
        public async Task<List<ProductCatalogResult>> GetAllAsync()
        {
            var catalogs = await _repository.Query()
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            return _mapper.Map<List<ProductCatalogResult>>(
                catalogs.Where(c => !c.IsDeleted)
            );
        }

        // =========================
        // UPDATE (SuperAdmin only)
        // =========================
        public async Task<ProductCatalogResult> UpdateAsync(
            Guid id,
            UpdateProductCatalogRequest request)
        {
            var userId = _tenantContext.UserId;

            // =========================
            // GET EXISTING CATALOG
            // =========================

            var catalog = await _repository
                .Query()
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    !c.IsDeleted);

            if (catalog == null)
                throw new NotFoundException("ProductCatalog", id);


            // =========================
            // EFFECTIVE VALUES
            // Partial update support
            // =========================

            var effectiveIsPack =
                request.IsPack ?? catalog.IsPack;

            var effectiveSellingMode =
                request.SellingMode ?? catalog.SellingMode;

            var effectiveUnitOfMeasure =
                request.UnitOfMeasure ?? catalog.UnitOfMeasure;


            // =========================
            // PACK VALIDATION
            // =========================

            if (effectiveIsPack &&
                request.PackComponents != null &&
                request.PackComponents.Any(x => x.Quantity <= 0))
            {
                throw new ValidationException(
                    "Pack component quantity must be > 0");
            }


            // =========================
            // SELLING MODE VALIDATION
            // =========================

            if (effectiveSellingMode == SellingMode.Weight &&
                effectiveUnitOfMeasure == "pcs")
            {
                throw new ValidationException(
                    "Weight products cannot use pcs");
            }

            if (effectiveSellingMode == SellingMode.Unit &&
                effectiveUnitOfMeasure != "pcs")
            {
                throw new ValidationException(
                    "Unit products must use pcs");
            }


            // =========================
            // INTERNAL CODE
            // =========================

            if (!string.IsNullOrWhiteSpace(request.InternalCode))
            {
                request.InternalCode =
                    request.InternalCode.Trim();

                if (request.InternalCode != catalog.InternalCode)
                {
                    var exists = await _repository.ExistsAsync(c =>
                        c.InternalCode == request.InternalCode &&
                        c.Id != id &&
                        !c.IsDeleted);

                    if (exists)
                    {
                        throw new ConflictException(
                            "InternalCode already used.");
                    }
                }
            }


            // =========================
            // BARCODE
            // null = do not modify
            // =========================

            if (request.Barcode != null)
            {
                if (!string.IsNullOrWhiteSpace(request.Barcode))
                {
                    request.Barcode = new string(
                        request.Barcode
                            .Trim()
                            .Where(char.IsLetterOrDigit)
                            .ToArray());

                    request.Barcode =
                        EanTools.Normalize(request.Barcode);

                    if (request.Barcode != catalog.Barcode)
                    {
                        var type =
                            BarcodeDetector.Detect(request.Barcode);

                        if (!BarcodeValidator.IsValid(
                                request.Barcode,
                                type))
                        {
                            throw new ValidationException(
                                "Invalid barcode");
                        }

                        // Barcode must be unique globally
                        // among active ProductCatalog rows.
                        var barcodeExists =
                            await _repository.ExistsAsync(c =>
                                c.Barcode == request.Barcode &&
                                c.Id != id &&
                                !c.IsDeleted);

                        if (barcodeExists)
                        {
                            throw new ConflictException(
                                "Barcode already used.");
                        }

                        catalog.BarcodeType = type;
                    }
                }
                else
                {
                    // Empty string is treated as:
                    // no barcode value supplied to mapping.
                    //
                    // If later you want explicit barcode removal,
                    // use a dedicated flag/action.
                    request.Barcode = null;
                }
            }


            // =========================
            // MAP PARTIAL UPDATE
            // =========================

            _mapper.Map(request, catalog);


            // =========================
            // MANUAL FIELDS
            // =========================

            if (request.IsPack.HasValue)
            {
                catalog.IsPack =
                    request.IsPack.Value;
            }

            catalog.ModifiedAt =
                DateTime.UtcNow;

            catalog.ModifiedByUserId =
                userId;


            // =========================
            // PACK COMPONENTS
            // =========================
            //
            // Rules:
            //
            // PackComponents == null
            //      => do not modify existing components
            //
            // PackComponents supplied
            //      => replace existing components
            //
            // IsPack == false
            //      => remove existing components
            // =========================


            // Product explicitly converted
            // from Pack -> normal Product.
            if (request.IsPack == false)
            {
                var existingComponents =
                    await _packComponentRepository
                        .Query()
                        .Where(pc =>
                            pc.PackCatalaogId == id)
                        .ToListAsync();

                if (existingComponents.Any())
                {
                    _packComponentRepository
                        .DeleteRange(existingComponents);
                }
            }
            // Components were explicitly supplied.
            else if (request.PackComponents != null)
            {
                var existingComponents =
                    await _packComponentRepository
                        .Query()
                        .Where(pc =>
                            pc.PackCatalaogId == id)
                        .ToListAsync();

                if (existingComponents.Any())
                {
                    _packComponentRepository
                        .DeleteRange(existingComponents);
                }

                // Only recreate components
                // when the resulting product is a Pack.
                if (effectiveIsPack &&
                    request.PackComponents.Any())
                {
                    foreach (var comp
                             in request.PackComponents)
                    {
                        await _packComponentRepository
                            .AddAsync(
                                new PackComponent
                                {
                                    Id = Guid.NewGuid(),

                                    PackCatalaogId =
                                        id,

                                    ComponentCatalogId =
                                        comp.ComponentCatalogId,

                                    Quantity =
                                        comp.Quantity,

                                    CreatedAt =
                                        DateTime.UtcNow
                                });
                    }
                }
            }


            // =========================
            // SAVE
            // =========================

            _repository.Update(catalog);

            await _unitOfWork
                .SaveChangesAsync();


            // =========================
            // RESULT
            // =========================

            return _mapper.Map<ProductCatalogResult>(
                catalog);
        }

        // =========================
        // DELETE (SuperAdmin only)
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var catalog = await _repository.Query()
                .FirstOrDefaultAsync(c => c.Id == id
                    && !c.IsDeleted);

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
            if (query.Page < 1)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    {"Page", new[]{"Page must be >= 1"} }
                });
            }

            if (query.PageSize < 1 || query.PageSize > 1000)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    {"PageSize", new[]{"PageSize must be between 1 and 1000"} }
                });
            }

            var dbQuery = _repository.Query()
                .Where(x => !x.IsDeleted);

            // =========================
            // SEARCH
            // =========================
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                dbQuery = dbQuery.Where(p =>
                    EF.Functions.ILike(p.Name, $"%{search}%") ||
                    (p.Barcode != null && EF.Functions.ILike(p.Barcode, $"%{search}%")) ||
                    (p.InternalCode != null && EF.Functions.ILike(p.InternalCode, $"%{search}%")) ||
                    (p.Brand != null && EF.Functions.ILike(p.Brand, $"%{search}%"))
                );
            }

            // =========================
            // SORT (SAFE)
            // =========================
            var sortBy = query.SortBy?.ToLower();

            dbQuery = sortBy switch
            {
                "name" => query.Desc
                    ? dbQuery.OrderByDescending(p => p.Name)
                    : dbQuery.OrderBy(p => p.Name),

                "barcode" => query.Desc
                    ? dbQuery.OrderByDescending(p => p.Barcode)
                    : dbQuery.OrderBy(p => p.Barcode),

                "internalcode" => query.Desc
                    ? dbQuery.OrderByDescending(p => p.InternalCode)
                    : dbQuery.OrderBy(p => p.InternalCode),

                _ => query.Desc
                    ? dbQuery.OrderByDescending(p => p.CreatedAt)
                    : dbQuery.OrderBy(p => p.CreatedAt),
            };

            // =========================
            // COUNT
            // =========================
            var total = await dbQuery.CountAsync();

            // =========================
            // DATA
            // =========================
            var items = await dbQuery
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
