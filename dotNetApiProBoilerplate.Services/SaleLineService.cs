using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.SaleLines.Requests;
using Inventory.Dto.SaleLines.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class SaleLineService
    {
        private readonly IRepository<SaleLine> _repository;
        private readonly IRepository<Sale> _saleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public SaleLineService(
            IRepository<SaleLine> repository,
            IRepository<Sale> saleRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _saleRepository = saleRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
        }

        // CREATE
        public async Task<SaleLineResult> CreateAsync(CreateSaleLineRequest request)
        {
            var tenantId = _tenantContext.TenantId;

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

            // Verify parent Sale belongs to tenant
            var sale = await _saleRepository.GetByIdAsync(request.SaleId);
            if (sale == null || sale.TenantId != tenantId)
            {
                throw new NotFoundException("Sale", request.SaleId);
            }

            var saleLine = _mapper.Map<SaleLine>(request);
            saleLine.Id = Guid.NewGuid();
            saleLine.TenantId = tenantId;
            saleLine.VatRate = request.VatRate;

            await _repository.AddAsync(saleLine);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleLineResult>(saleLine);
        }

        // GET BY ID
        public async Task<SaleLineResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var saleLine = await _repository.GetByIdAsync(id);

            if (saleLine == null)
            {
                throw new NotFoundException("SaleLine", id);
            }

            // Verify through parent Sale
            var sale = await _saleRepository.GetByIdAsync(saleLine.SaleId);
            if (sale == null || sale.TenantId != tenantId)
            {
                throw new NotFoundException("SaleLine", id);
            }

            return _mapper.Map<SaleLineResult>(saleLine);
        }

        // GET ALL
        public async Task<List<SaleLineResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var saleLines = await _repository.GetAllAsync();
            var sales = await _saleRepository.GetAllAsync();

            var tenantSaleIds = sales
                .Where(s => s.TenantId == tenantId)
                .Select(s => s.Id)
                .ToHashSet();

            var filtered = saleLines
                .Where(sl => tenantSaleIds.Contains(sl.SaleId))
                .ToList();

            return _mapper.Map<List<SaleLineResult>>(filtered);
        }

        // UPDATE
        public async Task<SaleLineResult> UpdateAsync(Guid id, UpdateSaleLineRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var saleLine = await _repository.GetByIdAsync(id);

            if (saleLine == null)
            {
                throw new NotFoundException("SaleLine", id);
            }

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

            // Verify through parent Sale
            var sale = await _saleRepository.GetByIdAsync(saleLine.SaleId);
            if (sale == null || sale.TenantId != tenantId)
            {
                throw new NotFoundException("SaleLine", id);
            }

            _mapper.Map(request, saleLine);

            _repository.Update(saleLine);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleLineResult>(saleLine);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var saleLine = await _repository.GetByIdAsync(id);

            if (saleLine == null)
            {
                throw new NotFoundException("SaleLine", id);
            }

            // Verify through parent Sale
            var sale = await _saleRepository.GetByIdAsync(saleLine.SaleId);
            if (sale == null || sale.TenantId != tenantId)
            {
                throw new NotFoundException("SaleLine", id);
            }

            _repository.Update(saleLine);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<SaleLineResult>> QueryAsync(SaleLineQuery query)
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

            var saleLines = await _repository.GetAllAsync();
            var sales = await _saleRepository.GetAllAsync();

            var tenantSaleIds = sales
                .Where(s => s.TenantId == tenantId)
                .Select(s => s.Id)
                .ToHashSet();

            var filtered = saleLines
                .Where(sl => tenantSaleIds.Contains(sl.SaleId))
                .AsQueryable();

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
