using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Dto.Suppliers.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class SupplierService
    {
        private readonly IRepository<Supplier> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public SupplierService(
            IRepository<Supplier> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<SupplierResult> CreateAsync(CreateSupplierRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Name", new[] { "Supplier name must not be empty." } }
                });
            }

            var exists = await _repository.ExistsAsync(
                s => s.Name == request.Name &&
                     s.TenantId == tenantId &&
                     !s.IsDeleted);

            if (exists)
            {
                throw new ConflictException(
                    $"Supplier with name '{request.Name}' already exists.");
            }

            var supplier = _mapper.Map<Supplier>(request);

            supplier.Id = Guid.NewGuid();
            supplier.TenantId = tenantId;
            supplier.IsActive = true;
            supplier.CreatedAt = DateTime.UtcNow;
            supplier.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SupplierResult>(supplier);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<SupplierResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var supplier = await _repository.GetByIdAsync(id);

            if (supplier == null || supplier.IsDeleted || supplier.TenantId != tenantId)
            {
                throw new NotFoundException("Supplier", id);
            }

            return _mapper.Map<SupplierResult>(supplier);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<SupplierResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.GetTenantId();
            var suppliers = await _repository.GetAllAsync();

            return _mapper.Map<List<SupplierResult>>(
                suppliers.Where(s => !s.IsDeleted && s.TenantId == tenantId).ToList()
            );
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<SupplierResult> UpdateAsync(Guid id, UpdateSupplierRequest request)
        {
            var tenantId = _tenantContext.GetTenantId();
            var supplier = await _repository.GetByIdAsync(id);

            if (supplier == null || supplier.IsDeleted || supplier.TenantId != tenantId)
            {
                throw new NotFoundException("Supplier", id);
            }

            if (!string.IsNullOrWhiteSpace(request.Name) &&
                request.Name != supplier.Name)
            {
                var nameExists = await _repository.ExistsAsync(
                    s => s.Name == request.Name &&
                         s.Id != id &&
                         s.TenantId == tenantId &&
                         !s.IsDeleted);

                if (nameExists)
                {
                    throw new ConflictException(
                        $"Supplier with name '{request.Name}' already exists.");
                }
            }

            _mapper.Map(request, supplier);

            supplier.ModifiedAt = DateTime.UtcNow;

            _repository.Update(supplier);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SupplierResult>(supplier);
        }

        // =========================
        // DELETE (soft)
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.GetTenantId();
            var supplier = await _repository.GetByIdAsync(id);

            if (supplier == null || supplier.IsDeleted || supplier.TenantId != tenantId)
            {
                throw new NotFoundException("Supplier", id);
            }

            supplier.IsDeleted = true;
            supplier.ModifiedAt = DateTime.UtcNow;

            _repository.Update(supplier);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<SupplierResult>> QueryAsync(SupplierQuery query)
        {
            var tenantId = _tenantContext.GetTenantId();

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var suppliers = (await _repository.GetAllAsync())
                .Where(s => !s.IsDeleted && s.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                suppliers = suppliers.Where(s =>
                    s.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    (s.Email != null && s.Email.Contains(query.Search, StringComparison.OrdinalIgnoreCase)) ||
                    (s.Phone != null && s.Phone.Contains(query.Search, StringComparison.OrdinalIgnoreCase)) ||
                    (s.TaxNumber != null && s.TaxNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase)));
            }

            if (query.IsActive.HasValue)
            {
                suppliers = suppliers.Where(s => s.IsActive == query.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Country))
            {
                suppliers = suppliers.Where(s => s.Country == query.Country);
            }

            if (!string.IsNullOrWhiteSpace(query.City))
            {
                suppliers = suppliers.Where(s => s.City == query.City);
            }

            suppliers = query.SortBy.ToLower() switch
            {
                "name" => query.Desc
                    ? suppliers.OrderByDescending(s => s.Name)
                    : suppliers.OrderBy(s => s.Name),

                "bankaccount" => query.Desc
                    ? suppliers.OrderByDescending(s => s.BankAccount)
                    : suppliers.OrderBy(s => s.BankAccount),

                _ => query.Desc
                    ? suppliers.OrderByDescending(s => s.CreatedAt)
                    : suppliers.OrderBy(s => s.CreatedAt)
            };

            var total = suppliers.Count();

            var items = suppliers
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<SupplierResult>
            {
                Items = _mapper.Map<List<SupplierResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
