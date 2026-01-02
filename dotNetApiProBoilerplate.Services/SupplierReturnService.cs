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

namespace Inventory.Services
{
    public class SupplierReturnService
    {
        private readonly IRepository<SupplierReturn> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SupplierReturnService(
            IRepository<SupplierReturn> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<SupplierReturnResult> CreateAsync(CreateSupplierReturnRequest request)
        {
            var exists = await _repository.ExistsAsync(r =>
                r.ReturnNumber == request.ReturnNumber && !r.IsDeleted);

            if (exists)
            {
                throw new ConflictException(
                    $"Supplier return '{request.ReturnNumber}' already exists.");
            }

            var entity = _mapper.Map<SupplierReturn>(request);

            entity.Id = Guid.NewGuid();
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
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
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
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
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
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
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
            var supplierReturns = await _repository.GetAllAsync();

            return _mapper.Map<List<SupplierReturnResult>>(
                supplierReturns.Where(s => !s.IsDeleted).ToList());
        }
        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<SupplierReturnResult>> QueryAsync(SupplierReturnQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var returns = (await _repository.GetAllAsync())
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            // Filters
            if (query.SupplierId.HasValue)
                returns = returns.Where(r => r.SupplierId == query.SupplierId.Value);

            if (query.Status.HasValue)
                returns = returns.Where(r => r.Status == (Domain.Enums.SupplierReturnStatus)query.Status.Value);

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

            // Sorting
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
