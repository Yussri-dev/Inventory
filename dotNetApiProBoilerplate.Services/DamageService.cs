using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.Damages.Requests;
using Inventory.Dto.Damages.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class DamageService
    {
        private readonly IRepository<Damage> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDocumentNumberService _documentNumberService;

        public DamageService(
            IRepository<Damage> repository,
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
        public async Task<DamageResult> CreateAsync(CreateDamageRequest request)
        {
            if (request.Quantity <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Quantity", new[] { "Quantity must be greater than 0." } }
                });
            }

            var entity = _mapper.Map<Damage>(request);

            entity.Id = Guid.NewGuid();
            entity.DamageNumber = await _documentNumberService.GenerateAsync("DAMAGE");
            entity.DamageDate = DateTime.UtcNow;
            entity.IsApproved = false;

            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DamageResult>(entity);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<DamageResult> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("Damage", id);

            return _mapper.Map<DamageResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<DamageResult>> GetAllAsync()
        {
            var damages = await _repository.GetAllAsync();

            return _mapper.Map<List<DamageResult>>(
                damages.Where(x => !x.IsDeleted).ToList()
            );
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<DamageResult> UpdateAsync(Guid id, UpdateDamageRequest request)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("Damage", id);

            _mapper.Map(request, entity);
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DamageResult>(entity);
        }

        // =========================
        // APPROVE
        // =========================
        public async Task<DamageResult> ApproveAsync(Guid id, Guid approvedByUserId)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("Damage", id);

            if (entity.IsApproved)
                throw new ConflictException("Damage is already approved.");

            entity.IsApproved = true;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByUserId = approvedByUserId;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DamageResult>(entity);
        }

        // =========================
        // SOFT DELETE
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("Damage", id);

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<DamageResult>> QueryAsync(DamageQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var source = (await _repository.GetAllAsync())
                .Where(d => !d.IsDeleted)
                .AsQueryable();

            if (query.ProductId.HasValue)
                source = source.Where(d => d.ProductId == query.ProductId.Value);

            if (query.IsApproved.HasValue)
                source = source.Where(d => d.IsApproved == query.IsApproved.Value);

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
                "damagedate" => query.Desc
                    ? source.OrderByDescending(d => d.DamageDate)
                    : source.OrderBy(d => d.DamageDate),

                "quantity" => query.Desc
                    ? source.OrderByDescending(d => d.Quantity)
                    : source.OrderBy(d => d.Quantity),

                _ => query.Desc
                    ? source.OrderByDescending(d => d.CreatedAt)
                    : source.OrderBy(d => d.CreatedAt)
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
