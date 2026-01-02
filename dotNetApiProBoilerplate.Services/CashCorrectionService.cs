using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CashCorrections.Requests;
using Inventory.Dto.CashCorrections.Results;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;
using Microsoft.VisualBasic;

namespace Inventory.Services
{
    public class CashCorrectionService
    {
        private readonly IRepository<CashCorrection> _repository;
        private readonly IRepository<CashSession> _cashSessionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CashCorrectionService(
            IRepository<CashCorrection> repository,
            IRepository<CashSession> cashSessionRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _cashSessionRepository = cashSessionRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        // =========================
        // CREATE
        // =========================
        public async Task<CashCorrectionResult> CreateAsync(CreateCashCorrectionRequest request)
        {
            if (request.Amount == 0)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Amount", new[] { "Amount cannot be zero." } }
                });

            var session = await _cashSessionRepository.GetByIdAsync(request.OriginalCashSessionId);

            if (session == null || session.Status != (Domain.Enums.CashSessionStatus)CashSessionStatus.Closed)
                throw new ConflictException("Cash session must be closed to apply a correction.");

            var entity = _mapper.Map<CashCorrection>(request);

            entity.Id = Guid.NewGuid();
            entity.CorrectedAt = DateTime.UtcNow;
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashCorrectionResult>(entity);
        }


        // =========================
        // GET ALL
        // =========================
        public async Task<List<CashCorrectionResult>> GetAllAsync()
        {
            var cashCorrections = await _repository.GetAllAsync();
            
            var activecashCorrections = cashCorrections.Where(x => x!.IsDeleted).ToList();

            return _mapper.Map<List<CashCorrectionResult>>(activecashCorrections);
        }

        // =========================
        // GET ById
        // =========================
        public async Task<CashCorrectionResult> GetByIdAsync(Guid id)
        {
            var cashCorrection = await _repository.GetByIdAsync(id);

            if (cashCorrection == null || cashCorrection.IsDeleted)
            {
                throw new NotFoundException("CashCorrection", id);
            }
            return _mapper.Map<CashCorrectionResult>(cashCorrection);
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<CashCorrectionResult> UpdateAsync(Guid id, UpdateCashCorrectionRequest request)
        {
            var cashCorrection = await _repository.GetByIdAsync(id);
            if (cashCorrection == null || cashCorrection.IsDeleted)
            {
                throw new NotFoundException("cashCorrection", id);
            }

            _mapper.Map(request, cashCorrection);

            cashCorrection.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashCorrection);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashCorrectionResult>(cashCorrection);
        }

        // =========================
        // DELETE
        // =========================

        public async Task<bool> DeleteAsync(Guid id)
        {
            var cashCorrection = await _repository.GetByIdAsync(id);
            if (cashCorrection == null || cashCorrection.IsDeleted)
            {
                throw new NotFoundException("cashCorrection", id);
            }

            cashCorrection.IsDeleted = true;
            cashCorrection.ModifiedAt = DateTime.UtcNow;

            _repository.Update(cashCorrection);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // =========================
        // APPROVE
        // =========================
        public async Task<CashCorrectionResult> ApproveAsync(Guid id, Guid approvedByUserId, string? notes)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                throw new NotFoundException("CashCorrection", id);

            if (entity.ApprovedAt != default)
                throw new ConflictException("Cash correction already approved.");

            entity.ApprovedByUserId = approvedByUserId;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovalNotes = notes;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CashCorrectionResult>(entity);
        }

        // =========================
        // QUERY
        // =========================
        public async Task<PagedResult<CashCorrectionResult>> QueryAsync(CashCorrectionQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });

            var source = (await _repository.GetAllAsync())
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
                source = source.Where(x => x.Reason.Contains(query.Search));

            source = query.SortBy.ToLower() switch
            {
                "amount" => query.Desc
                    ? source.OrderByDescending(x => x.Amount)
                    : source.OrderBy(x => x.Amount),

                "correctedat" => query.Desc
                    ? source.OrderByDescending(x => x.CorrectedAt)
                    : source.OrderBy(x => x.CorrectedAt),

                _ => query.Desc
                    ? source.OrderByDescending(x => x.CreatedAt)
                    : source.OrderBy(x => x.CreatedAt)
            };

            var total = source.Count();

            var items = source
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<CashCorrectionResult>
            {
                Items = _mapper.Map<List<CashCorrectionResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
