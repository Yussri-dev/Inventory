using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Returns.Requests;
using Inventory.Dto.Returns.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class ReturnService
    {
        private readonly IRepository<Return> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDocumentNumberService _documentNumberService;

        public ReturnService(
            IRepository<Return> repository,
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
        public async Task<ReturnResult> CreateAsync(CreateReturnRequest request)
        {
            if (request.TotalAmount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "TotalAmount", new[] { "TotalAmount must be > 0." } }
                });
            }

            var entity = _mapper.Map<Return>(request);

            entity.Id = Guid.NewGuid();
            entity.ReturnNumber = await _documentNumberService.GenerateAsync("RETURN");
            entity.ReturnDate = request.ReturnDate == default
                ? DateTime.UtcNow
                : request.ReturnDate;

            entity.TotalAmount = request.TotalAmount;
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReturnResult>(entity);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<ReturnResult> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity is null || entity.IsDeleted)
                throw new NotFoundException("Return", id);

            return _mapper.Map<ReturnResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<ReturnResult>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();

            return _mapper.Map<List<ReturnResult>>(
                items.Where(x => !x.IsDeleted).ToList()
            );
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<ReturnResult> UpdateAsync(Guid id, UpdateReturnRequest request)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity is null || entity.IsDeleted)
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
            var entity = await _repository.GetByIdAsync(id);

            if (entity is null || entity.IsDeleted)
                throw new NotFoundException("Return", id);

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY (pagination / filter / sort)
        // =========================
        public async Task<PagedResult<ReturnResult>> QueryAsync(ReturnQuery query)
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
                    x.ReturnNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
            }

            source = query.SortBy?.ToLower() switch
            {
                "returnnumber" => query.Desc
                    ? source.OrderByDescending(x => x.ReturnNumber)
                    : source.OrderBy(x => x.ReturnNumber),

                "returndate" => query.Desc
                    ? source.OrderByDescending(x => x.ReturnDate)
                    : source.OrderBy(x => x.ReturnDate),

                _ => query.Desc
                    ? source.OrderByDescending(x => x.CreatedAt)
                    : source.OrderBy(x => x.CreatedAt)
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
