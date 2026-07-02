using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Microsoft.EntityFrameworkCore;
namespace Inventory.LocalDB.Services.Sync
{
    public class SyncQueueService : ISyncQueueService
    {
        private readonly PosLocalDbContext _db;

        public SyncQueueService(PosLocalDbContext db)
        {
            _db = db;
        }

        public async Task EnqueueAsync(
            string entityName,
            Guid localEntityId,
            string operation,
            string payloadJson,
            Guid? serverEntityId = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name is required.", nameof(entityName));

            if (string.IsNullOrWhiteSpace(operation))
                throw new ArgumentException("Operation is required.", nameof(operation));

            if (string.IsNullOrWhiteSpace(payloadJson))
                throw new ArgumentException("Payload JSON is required.", nameof(payloadJson));

            var item = new SyncQueueItem
            {
                Id = Guid.NewGuid(),
                EntityName = entityName,
                LocalEntityId = localEntityId,
                ServerEntityId = serverEntityId,
                Operation = operation,
                PayloadJson = payloadJson,
                Status = SyncQueueStatus.Pending,
                Attempts = 0,
                CreatedAtUtc = DateTime.UtcNow,
                ClientOperationId = Guid.NewGuid()
            };

            _db.SyncQueueItems.Add(item);
            await _db.SaveChangesAsync();
        }

        public async Task<List<SyncQueueItem>> GetPendingAsync(int take = 50)
        {
            return await _db.SyncQueueItems
                .Where(x =>
                    x.Status == SyncQueueStatus.Pending ||
                    x.Status == SyncQueueStatus.Failed)
                .OrderBy(x => x.CreatedAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<SyncQueueItem>> GetConflictsAsync()
        {
            return await _db.SyncQueueItems
                .Where(x => x.Status == SyncQueueStatus.Conflict)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task MarkProcessingAsync(Guid id)
        {
            var item = await GetRequiredAsync(id);

            item.Status = SyncQueueStatus.Processing;
            item.Attempts++;
            item.LastAttemptAtUtc = DateTime.UtcNow;
            item.ErrorMessage = null;

            await _db.SaveChangesAsync();
        }

        public async Task MarkDoneAsync(Guid id, Guid? serverEntityId = null)
        {
            var item = await GetRequiredAsync(id);

            item.Status = SyncQueueStatus.Done;
            item.ServerEntityId = serverEntityId ?? item.ServerEntityId;
            item.ProcessedAtUtc = DateTime.UtcNow;
            item.ErrorMessage = null;

            await _db.SaveChangesAsync();
        }

        public async Task MarkFailedAsync(Guid id, string errorMessage)
        {
            var item = await GetRequiredAsync(id);

            item.Status = SyncQueueStatus.Failed;
            item.ErrorMessage = errorMessage;
            item.LastAttemptAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task MarkConflictAsync(Guid id, string errorMessage)
        {
            var item = await GetRequiredAsync(id);

            item.Status = SyncQueueStatus.Conflict;
            item.ErrorMessage = errorMessage;
            item.LastAttemptAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        private async Task<SyncQueueItem> GetRequiredAsync(Guid id)
        {
            var item = await _db.SyncQueueItems.FirstOrDefaultAsync(x => x.Id == id);

            if (item == null)
                throw new InvalidOperationException($"Sync queue item '{id}' was not found.");

            return item;
        }
    }
}
