using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.SaleReceipts.Requests;
using Inventory.Dto.SaleReceipts.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class SaleReceiptService
    {
        private readonly IRepository<SaleReceipt> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SaleReceiptService(
            IRepository<SaleReceipt> repository,
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
        public async Task<SaleReceiptResult> CreateAsync(CreateSaleReceiptRequest request)
        {
            var exists = await _repository.ExistsAsync(r =>
                r.ReceiptNumber == request.ReceiptNumber && !r.IsDeleted);

            if (exists)
            {
                throw new ConflictException(
                    $"Receipt '{request.ReceiptNumber}' already exists.");
            }

            var entity = _mapper.Map<SaleReceipt>(request);

            entity.Id = Guid.NewGuid();
            entity.GeneratedAt = DateTime.UtcNow;
            entity.IsPrinted = false;
            entity.IsEmailed = false;
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SaleReceiptResult>(entity);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<SaleReceiptResult> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("SaleReceipt", id);

            return _mapper.Map<SaleReceiptResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<SaleReceiptResult>> GetAllAsync()
        {
            var receipts = await _repository.GetAllAsync();

            return _mapper.Map<List<SaleReceiptResult>>(
                receipts.Where(r => !r.IsDeleted).ToList()
            );
        }

        // =========================
        // MARK AS PRINTED
        // =========================
        public async Task<bool> MarkAsPrintedAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("SaleReceipt", id);

            entity.IsPrinted = true;
            entity.PrintedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // MARK AS EMAILED
        // =========================
        public async Task<bool> MarkAsEmailedAsync(Guid id, string email)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("SaleReceipt", id);

            entity.IsEmailed = true;
            entity.EmailedAt = DateTime.UtcNow;
            entity.EmailAddress = email;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // SOFT DELETE
        // =========================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("SaleReceipt", id);

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<SaleReceiptResult>> QueryAsync(SaleReceiptQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var receipts = (await _repository.GetAllAsync())
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            if (query.SaleId.HasValue)
                receipts = receipts.Where(r => r.SaleId == query.SaleId.Value);

            if (query.IsPrinted.HasValue)
                receipts = receipts.Where(r => r.IsPrinted == query.IsPrinted.Value);

            if (query.IsEmailed.HasValue)
                receipts = receipts.Where(r => r.IsEmailed == query.IsEmailed.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                receipts = receipts.Where(r =>
                    r.ReceiptNumber.Contains(query.Search) ||
                    (r.EmailAddress != null && r.EmailAddress.Contains(query.Search)));
            }

            receipts = query.SortBy.ToLower() switch
            {
                "receiptnumber" => query.Desc
                    ? receipts.OrderByDescending(r => r.ReceiptNumber)
                    : receipts.OrderBy(r => r.ReceiptNumber),

                "generatedat" => query.Desc
                    ? receipts.OrderByDescending(r => r.GeneratedAt)
                    : receipts.OrderBy(r => r.GeneratedAt),

                _ => query.Desc
                    ? receipts.OrderByDescending(r => r.CreatedAt)
                    : receipts.OrderBy(r => r.CreatedAt)
            };

            var total = receipts.Count();

            var items = receipts
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<SaleReceiptResult>
            {
                Items = _mapper.Map<List<SaleReceiptResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
