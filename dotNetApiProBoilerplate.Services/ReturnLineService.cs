using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.ReturnLines.Requests;
using Inventory.Dto.ReturnLines.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class ReturnLineService
    {
        private readonly IRepository<ReturnLine> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReturnLineService(
            IRepository<ReturnLine> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CREATE
        // CREATE
        public async Task<ReturnLineResult> CreateAsync(CreateReturnLineRequest request)
        {
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
            var purchaseLine = await _repository.GetByIdAsync(id);

            //if (purchaseLine is null || purchaseLine.IsDeleted)
            //{
            //    throw new NotFoundException("ReturnLine", id);
            //}

            return _mapper.Map<ReturnLineResult>(purchaseLine);
        }

        // GET ALL
        public async Task<List<ReturnLineResult>> GetAllAsync()
        {
            var purchaseLines = await _repository.GetAllAsync();

            //// Filter out soft-deleted purchaseLines
            //var activeReturnLines = purchaseLines.Where(p => !p.IsDeleted).ToList();

            return _mapper.Map<List<ReturnLineResult>>(purchaseLines);
        }

        // UPDATE
        public async Task<ReturnLineResult> UpdateAsync(Guid id, UpdateReturnLineRequest request)
        {
            var purchaseLine = await _repository.GetByIdAsync(id);

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

            _mapper.Map(request, purchaseLine);

            _repository.Update(purchaseLine);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReturnLineResult>(purchaseLine);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var purchaseLine = await _repository.GetByIdAsync(id);

            _repository.Update(purchaseLine);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<ReturnLineResult>> QueryAsync(ReturnLineQuery query)
        {
            // Validate query parameters
            if (query.Page < 1)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Page", new[] { "Page must be greater than or equal to 1." } }
                };
                throw new ValidationException(errors);
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                };
                throw new ValidationException(errors);
            }

            var filtered = await _repository.GetAllAsync();

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