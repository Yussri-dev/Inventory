using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.SaleLines.Requests;
using Inventory.Dto.SaleLines.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class SaleLineService
    {
        private readonly IRepository<SaleLine> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SaleLineService(
            IRepository<SaleLine> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CREATE
        // CREATE
        public async Task<SaleLineResult> CreateAsync(CreateSaleLineRequest request)
        {
            if (request.Quantity <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "QuantityOrdered", new[] { "QuantityOrdered must be > 0." } }
                });

            }

            if (request.LineAmountExclVat <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "LineAmountExclVat", new[] { "LineAmountExclVat must be > 0." } }
                });
            }

            if (request.LineAmountInclVat <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "LineAmountInclVat", new[] { "LineAmountInclVat must be > 0." } }
                });
            }

            var purchaseLine = _mapper.Map<SaleLine>(request);

            purchaseLine.Id = Guid.NewGuid();
            purchaseLine.VatRate = request.VatRate;

            await _repository.AddAsync(purchaseLine);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleLineResult>(purchaseLine);
        }

        // GET BY ID
        public async Task<SaleLineResult> GetByIdAsync(Guid id)
        {
            var purchaseLine = await _repository.GetByIdAsync(id);

            //if (purchaseLine is null || purchaseLine.IsDeleted)
            //{
            //    throw new NotFoundException("SaleLine", id);
            //}

            return _mapper.Map<SaleLineResult>(purchaseLine);
        }

        // GET ALL
        public async Task<List<SaleLineResult>> GetAllAsync()
        {
            var purchaseLines = await _repository.GetAllAsync();

            //// Filter out soft-deleted purchaseLines
            //var activeSaleLines = purchaseLines.Where(p => !p.IsDeleted).ToList();

            return _mapper.Map<List<SaleLineResult>>(purchaseLines);
        }

        // UPDATE
        public async Task<SaleLineResult> UpdateAsync(Guid id, UpdateSaleLineRequest request)
        {
            var purchaseLine = await _repository.GetByIdAsync(id);

            if (request.Quantity <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "QuantityOrdered", new[] { "QuantityOrdered must be > 0." } }
                });

            }

            if (request.LineAmountInclVat <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "LineAmountInclVat", new[] { "LineAmountInclVat must be > 0." } }
                });
            }

            if (request.LineAmountExclVat <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "LineAmountExclVat", new[] { "LineAmountExclVat must be > 0." } }
                });
            }

            _mapper.Map(request, purchaseLine);

            _repository.Update(purchaseLine);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleLineResult>(purchaseLine);
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
        public async Task<PagedResult<SaleLineResult>> QueryAsync(SaleLineQuery query)
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

            return new PagedResult<SaleLineResult>
            {
                Items = _mapper.Map<List<SaleLineResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}