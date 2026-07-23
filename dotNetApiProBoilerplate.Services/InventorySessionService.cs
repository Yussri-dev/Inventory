using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Models;
using Inventory.Dto.InventorySessions.Requests;
using Inventory.Dto.InventorySessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class InventorySessionService
    {
        private readonly IRepository<InventorySession> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;
        public readonly IRepository<InventoryLine> _line;
        public readonly IRepository<Stock> _stock;


        public InventorySessionService(
            IRepository<InventorySession> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext,
            IRepository<InventoryLine> line,
            IRepository<Stock> stock
            )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
            _line = line;
            _stock = stock;
        }

        // CREATE
        public async Task<InventorySessionResult> CreateAsync(CreateInventorySessionRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var exists = await _repository.ExistsAsync(s =>
                s.SessionNumber == request.SessionNumber &&
                s.TenantId == tenantId &&
                !s.IsDeleted);

            if (exists)
                throw new ConflictException(
                    $"Inventory session '{request.SessionNumber}' already exists.");

            var entity = _mapper.Map<InventorySession>(request);

            entity.Id = Guid.NewGuid();
            entity.TenantId = tenantId;
            entity.CreatedByUserId = userId;
            entity.UserId = userId;
            entity.Status = InventoryStatus.InProgress;
            entity.StartedAt = DateTime.UtcNow;
            entity.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InventorySessionResult>(entity);
        }

        // GET BY ID
        public async Task<InventorySessionResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;

            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("InventorySession", id);

            return _mapper.Map<InventorySessionResult>(entity);
        }

        // GET ALL
        public async Task<List<InventorySessionResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var sessions = await _repository.GetAllAsync();

            return _mapper.Map<List<InventorySessionResult>>(
                sessions.Where(s => !s.IsDeleted && s.TenantId == tenantId).ToList());
        }

        // UPDATE — only InProgress allowed
        public async Task<InventorySessionResult> UpdateAsync(Guid id, UpdateInventorySessionRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("InventorySession", id);

            if (entity.Status != InventoryStatus.InProgress)
                throw new ConflictException("Only open sessions can be modified.");

            _mapper.Map(request, entity);

            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedByUserId = userId;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<InventorySessionResult>(entity);
        }

        // CLOSE
        public async Task<bool> CloseAsync(
     Guid id,
     CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            var entity =
                await _repository.GetByIdAsync(id);

            if (entity == null ||
                entity.IsDeleted ||
                entity.TenantId != tenantId)
            {
                throw new NotFoundException(
                    "InventorySession",
                    id);
            }

            /*
             * Idempotence:
             * la session a déjà été clôturée et le client répète
             * la requête après une perte de réponse HTTP.
             */
            if (entity.Status ==
                InventoryStatus.Completed)
            {
                return true;
            }

            /*
             * Une session validée a forcément déjà passé l'étape
             * de clôture. Il ne faut pas modifier ses données.
             */
            if (entity.Status ==
                InventoryStatus.Validated)
            {
                return true;
            }

            if (entity.Status !=
                InventoryStatus.InProgress)
            {
                throw new ConflictException(
                    "Only an inventory session in progress can be closed.");
            }

            var now =
                DateTime.UtcNow;

            entity.Status =
                InventoryStatus.Completed;

            entity.ClosedAt =
                now;

            entity.ModifiedAt =
                now;

            entity.ModifiedByUserId =
                userId;

            _repository.Update(
                entity);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // VALIDATE 

        public async Task<bool> ValidateAsync(
    Guid id,
    CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            var session =
                await _repository.GetByIdAsync(id);

            if (session == null ||
                session.IsDeleted ||
                session.TenantId != tenantId)
            {
                throw new NotFoundException(
                    "InventorySession",
                    id);
            }

            /*
             * Idempotence:
             * the server may have completed the validation while the client
             * failed to receive the HTTP response.
             */
            if (session.Status ==
                InventoryStatus.Validated)
            {
                return true;
            }

            if (session.Status !=
                InventoryStatus.Completed)
            {
                throw new ConflictException(
                    "Inventory session must be closed before validation.");
            }

            var lines =
                await _line.Query()
                    .Where(line =>
                        line.InventorySessionId == id &&
                        line.TenantId == tenantId &&
                        !line.IsDeleted)
                    .ToListAsync(
                        cancellationToken);

            /*
             * Only retrieve stocks required by lines that still need an
             * adjustment. This also prevents one database query per line.
             */
            var productIds =
                lines
                    .Where(line =>
                        !line.IsAdjusted)
                    .Select(line =>
                        line.ProductId)
                    .Distinct()
                    .ToList();

            var stocks =
                productIds.Count == 0
                    ? new List<Stock>()
                    : await _stock.Query()
                        .Where(stock =>
                            stock.TenantId == tenantId &&
                            !stock.IsDeleted &&
                            productIds.Contains(
                                stock.ProductId))
                        .ToListAsync(
                            cancellationToken);

            /*
             * More than one stock row for the same product and tenant is a
             * database inconsistency and must not be silently ignored.
             */
            var duplicateStock =
                stocks
                    .GroupBy(stock =>
                        stock.ProductId)
                    .FirstOrDefault(group =>
                        group.Count() > 1);

            if (duplicateStock != null)
            {
                throw new ConflictException(
                    $"Multiple stock records were found for product " +
                    $"{duplicateStock.Key}.");
            }

            var stocksByProduct =
                stocks.ToDictionary(
                    stock =>
                        stock.ProductId);

            /*
             * Validate all required stock records before changing any entity.
             */
            foreach (var line in lines.Where(line => !line.IsAdjusted))
            {
                if (!stocksByProduct.ContainsKey(
                        line.ProductId))
                {
                    throw new NotFoundException(
                        $"Stock record not found for product " +
                        $"{line.ProductId}.");
                }
            }

            var now =
                DateTime.UtcNow;

            foreach (var line in lines)
            {
                if (line.IsAdjusted)
                {
                    continue;
                }

                var stock =
                    stocksByProduct[line.ProductId];

                /*
                 * Inventory validation defines the new authoritative physical
                 * quantity. It does not add the variance to the old quantity.
                 */
                stock.Quantity =
                    line.CountedQuantity;

                stock.LastUpdated =
                    now;

                _stock.Update(
                    stock);

                line.IsAdjusted =
                    true;

                line.AdjustedAt =
                    now;

                _line.Update(
                    line);
            }

            session.Status =
                InventoryStatus.Validated;

            session.ValidatedAt =
                now;

            session.ValidatedByUserId =
                userId;

            session.ModifiedAt =
                now;

            session.ModifiedByUserId =
                userId;

            _repository.Update(
                session);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        //public async Task<bool> ValidateAsync(Guid id)
        //{
        //    var tenantId = _tenantContext.TenantId;
        //    var userId = _tenantContext.UserId;

        //    var entity = await _repository.GetByIdAsync(id);

        //    if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
        //        throw new NotFoundException("InventorySession", id);

        //    if (entity.Status != InventoryStatus.Completed)
        //        throw new ConflictException("Inventory session must be closed before validation.");

        //    entity.Status = InventoryStatus.Validated;
        //    entity.ValidatedAt = DateTime.UtcNow;
        //    entity.ValidatedByUserId = userId;
        //    entity.ModifiedAt = DateTime.UtcNow;
        //    entity.ModifiedByUserId = userId;

        //    _repository.Update(entity);
        //    await _unitOfWork.SaveChangesAsync();

        //    return true;
        //}

        // DELETE — cannot delete validated sessions
        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var entity = await _repository.GetByIdAsync(id);

            if (entity == null || entity.IsDeleted || entity.TenantId != tenantId)
                throw new NotFoundException("InventorySession", id);

            if (entity.Status == InventoryStatus.Validated)
                throw new ConflictException("Validated sessions cannot be deleted.");

            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedByUserId = userId;
            entity.ModifiedAt = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // QUERY
        public async Task<PagedResult<InventorySessionResult>> QueryAsync(InventorySessionQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });

            var sessions = (await _repository.GetAllAsync())
                .Where(s => !s.IsDeleted && s.TenantId == tenantId)
                .AsQueryable();

            if (query.Status.HasValue)
                sessions = sessions.Where(s => s.Status == (InventoryStatus)query.Status.Value);

            if (query.UserId.HasValue)
                sessions = sessions.Where(s => s.UserId == query.UserId.Value);

            if (query.FromDate.HasValue)
                sessions = sessions.Where(s => s.StartedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                sessions = sessions.Where(s => s.StartedAt <= query.ToDate.Value);

            var sortBy = query.SortBy?.ToLower() ?? "startedat";

            sessions = sortBy switch
            {
                "sessionnumber" => query.Desc
                    ? sessions.OrderByDescending(s => s.SessionNumber)
                    : sessions.OrderBy(s => s.SessionNumber),

                "status" => query.Desc
                    ? sessions.OrderByDescending(s => s.Status)
                    : sessions.OrderBy(s => s.Status),

                _ => query.Desc
                    ? sessions.OrderByDescending(s => s.StartedAt)
                    : sessions.OrderBy(s => s.StartedAt)
            };

            var total = sessions.Count();

            var items = sessions
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<InventorySessionResult>
            {
                Items = _mapper.Map<List<InventorySessionResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}