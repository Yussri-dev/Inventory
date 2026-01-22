using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.ReturnLines.Requests;
using Inventory.Dto.ReturnLines.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class ReturnLineService
    {
        private readonly IRepository<ReturnLine> _repository;
        private readonly IRepository<Return> _returnRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public ReturnLineService(
            IRepository<ReturnLine> repository,
            IRepository<Return> returnRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _returnRepository = returnRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // CREATE
        public async Task<ReturnLineResult> CreateAsync(CreateReturnLineRequest request)
        {
            var tenantId = _tenantContext.TenantId;

            if (request.Quantity <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "QuantityOrdered", new[] { "QuantityOrdered must be > 0." } }
                });
            }

            if (request.LineAmount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "LineAmountInclVat", new[] { "LineAmountInclVat must be > 0." } }
                });
            }

            // Verify parent Return belongs to tenant
            var parentReturn = await _returnRepository.GetByIdAsync(request.ReturnId);
            if (parentReturn == null || parentReturn.TenantId != tenantId)
            {
                throw new NotFoundException("Return", request.ReturnId);
            }

            var purchaseLine = _mapper.Map<ReturnLine>(request);
            purchaseLine.Id = Guid.NewGuid();
            purchaseLine.VatRate = request.VatRate;

            await _repository.AddAsync(purchaseLine);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReturnLineResult>(purchaseLine);
        }

        // GET BY ID
        public async Task<ReturnLineResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var purchaseLine = await _repository.GetByIdAsync(id);

            if (purchaseLine == null)
            {
                throw new NotFoundException("ReturnLine", id);
            }

            // Verify through parent Return
            var parentReturn = await _returnRepository.GetByIdAsync(purchaseLine.ReturnId);
            if (parentReturn == null || parentReturn.TenantId != tenantId)
            {
                throw new NotFoundException("ReturnLine", id);
            }

            return _mapper.Map<ReturnLineResult>(purchaseLine);
        }

        // GET ALL
        public async Task<List<ReturnLineResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var returnLines = await _repository.GetAllAsync();
            var returns = await _returnRepository.GetAllAsync();

            var tenantReturnIds = returns
                .Where(r => r.TenantId == tenantId)
                .Select(r => r.Id)
                .ToHashSet();

            var filtered = returnLines
                .Where(rl => tenantReturnIds.Contains(rl.ReturnId))
                .ToList();

            return _mapper.Map<List<ReturnLineResult>>(filtered);
        }

        // UPDATE
        public async Task<ReturnLineResult> UpdateAsync(Guid id, UpdateReturnLineRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var purchaseLine = await _repository.GetByIdAsync(id);

            if (purchaseLine == null)
            {
                throw new NotFoundException("ReturnLine", id);
            }

            if (request.Quantity <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "QuantityOrdered", new[] { "QuantityOrdered must be > 0." } }
                });
            }

            if (request.LineAmount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "LineAmountInclVat", new[] { "LineAmountInclVat must be > 0." } }
                });
            }

            // Verify through parent Return
            var parentReturn = await _returnRepository.GetByIdAsync(purchaseLine.ReturnId);
            if (parentReturn == null || parentReturn.TenantId != tenantId)
            {
                throw new NotFoundException("ReturnLine", id);
            }

            _mapper.Map(request, purchaseLine);
            _repository.Update(purchaseLine);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReturnLineResult>(purchaseLine);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var purchaseLine = await _repository.GetByIdAsync(id);

            if (purchaseLine == null)
            {
                throw new NotFoundException("ReturnLine", id);
            }

            // Verify through parent Return
            var parentReturn = await _returnRepository.GetByIdAsync(purchaseLine.ReturnId);
            if (parentReturn == null || parentReturn.TenantId != tenantId)
            {
                throw new NotFoundException("ReturnLine", id);
            }

            _repository.Update(purchaseLine);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<ReturnLineResult>> QueryAsync(ReturnLineQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Page", new[] { "Page must be greater than or equal to 1." } }
                });
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                });
            }

            var returnLines = await _repository.GetAllAsync();
            var returns = await _returnRepository.GetAllAsync();

            var tenantReturnIds = returns
                .Where(r => r.TenantId == tenantId)
                .Select(r => r.Id)
                .ToHashSet();

            var filtered = returnLines
                .Where(rl => tenantReturnIds.Contains(rl.ReturnId))
                .AsQueryable();

            var total = filtered.Count();
            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<ReturnLineResult>
            {
                Items = _mapper.Map<List<ReturnLineResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
