using Inventory.Dto.Enums;
using Inventory.Dto.StockMouvements.Requests;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.Services
{
    public sealed class LocalStockMovementUploadService
        : ILocalStockMovementUploadService
    {
        private const string StockMovementEntityName =
            "StockMovement";

        private readonly PosLocalDbContext _db;
        private readonly IStockMovementApi _stockMovementApi;
        private readonly ILocalTenantContext _tenantContext;
        private readonly ILogger<LocalStockMovementUploadService>
            _logger;

        public LocalStockMovementUploadService(
            PosLocalDbContext db,
            IStockMovementApi stockMovementApi,
            ILocalTenantContext tenantContext,
            ILogger<LocalStockMovementUploadService> logger)
        {
            _db = db;
            _stockMovementApi = stockMovementApi;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<LocalStockMovementUploadResult>
            SyncPendingAsync(
                CancellationToken cancellationToken = default)
        {
            var result =
                new LocalStockMovementUploadResult();

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            await RecoverInterruptedItemsAsync(
                tenantId,
                cancellationToken);

            var queueItems =
                await _db.SyncQueueItems
                    .Where(item =>
                        item.TenantId == tenantId &&
                        item.EntityName ==
                            StockMovementEntityName &&
                        item.Status ==
                            SyncQueueStatus.Pending)
                    .OrderBy(item =>
                        item.CreatedAtUtc)
                    .Take(100)
                    .ToListAsync(
                        cancellationToken);

            result.TotalPending =
                queueItems.Count;

            foreach (var queueItem in queueItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await UploadOneAsync(
                        queueItem,
                        result,
                        cancellationToken);
                }
                catch (HttpRequestException)
                {
                    queueItem.Status =
                        SyncQueueStatus.Pending;

                    queueItem.ErrorMessage =
                        "API offline. The stock adjustment will be retried.";

                    result.Failed++;

                    result.Messages.Add(
                        queueItem.ErrorMessage);

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    throw;
                }
                catch (ApiException exception)
                {
                    queueItem.ErrorMessage =
                        Truncate(
                            exception.Content ??
                            exception.Message,
                            2000);

                    if (IsAuthenticationError(
                            exception) ||
                        IsTemporaryError(
                            exception))
                    {
                        queueItem.Status =
                            SyncQueueStatus.Pending;
                    }
                    else
                    {
                        queueItem.Status =
                            SyncQueueStatus.Conflict;
                    }

                    result.Failed++;

                    result.Messages.Add(
                        $"Stock adjustment sync failed: " +
                        $"{queueItem.ErrorMessage}");

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    queueItem.Status =
                        SyncQueueStatus.Failed;

                    queueItem.ErrorMessage =
                        Truncate(
                            exception
                                .GetBaseException()
                                .Message,
                            2000);

                    result.Failed++;

                    result.Messages.Add(
                        $"Stock adjustment sync failed: " +
                        $"{queueItem.ErrorMessage}");

                    _logger.LogError(
                        exception,
                        "Failed to synchronize local stock movement {QueueId}.",
                        queueItem.Id);

                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
            }

            return result;
        }

        private async Task UploadOneAsync(
            SyncQueueItem queueItem,
            LocalStockMovementUploadResult result,
            CancellationToken cancellationToken)
        {
            queueItem.Status =
                SyncQueueStatus.Processing;

            queueItem.Attempts++;

            queueItem.ErrorMessage =
                null;

            await _db.SaveChangesAsync(
                cancellationToken);

            var movement =
                await _db.StockMovements
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId ==
                                queueItem.TenantId &&
                            item.Id ==
                                queueItem.LocalEntityId,
                        cancellationToken);

            if (movement == null)
            {
                queueItem.Status =
                    SyncQueueStatus.Failed;

                queueItem.ErrorMessage =
                    "The local stock movement was not found.";

                result.Failed++;

                result.Messages.Add(
                    queueItem.ErrorMessage);

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            if (!string.Equals(
                    queueItem.Operation,
                    SyncOperation.Create,
                    StringComparison.OrdinalIgnoreCase))
            {
                queueItem.Status =
                    SyncQueueStatus.Failed;

                queueItem.ErrorMessage =
                    $"Unsupported stock movement operation: " +
                    $"{queueItem.Operation}";

                result.Failed++;

                result.Messages.Add(
                    queueItem.ErrorMessage);

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            if (!string.Equals(
                    movement.Type,
                    LocalStockMovementType.Adjustment,
                    StringComparison.OrdinalIgnoreCase))
            {
                queueItem.Status =
                    SyncQueueStatus.Failed;

                queueItem.ErrorMessage =
                    "Only manual adjustment movements may use the " +
                    "StockMovement outbox entity.";

                movement.SyncStatus =
                    SyncQueueStatus.Failed;

                result.Failed++;

                result.Messages.Add(
                    queueItem.ErrorMessage);

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            var productServerId =
                movement.ProductServerId;

            if (productServerId ==
                Guid.Empty)
            {
                var product =
                    await _db.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId ==
                                    queueItem.TenantId &&
                                item.Id ==
                                    movement.ProductLocalId &&
                                !item.IsDeletedLocally,
                            cancellationToken);

                productServerId =
                    product?.ServerId ??
                    Guid.Empty;
            }

            if (productServerId ==
                Guid.Empty)
            {
                queueItem.Status =
                    SyncQueueStatus.Pending;

                queueItem.ErrorMessage =
                    "The product must be synchronized before its " +
                    "stock adjustment.";

                movement.SyncStatus =
                    SyncQueueStatus.Pending;

                result.Skipped++;

                result.Messages.Add(
                    queueItem.ErrorMessage);

                await _db.SaveChangesAsync(
                    cancellationToken);

                return;
            }

            movement.ProductServerId =
                productServerId;

            var request =
                new CreateStockMouvementRequest
                {
                    ProductId =
                        productServerId,

                    QuantityChange =
                        movement.QuantityChange,

                    Type =
                        StockMovementType.Adjustment,

                    Notes =
                        BuildServerNotes(
                            movement)
                };

            var serverMovement =
                await _stockMovementApi.Create(
                    request,
                    cancellationToken);

            TryAssignServerId(
                movement,
                serverMovement);

            var now =
                DateTime.UtcNow;

            movement.SyncStatus =
                SyncQueueStatus.Done;

            movement.LastSyncedAtUtc =
                now;

            queueItem.Status =
                SyncQueueStatus.Done;

            queueItem.ProcessedAtUtc =
                now;

            queueItem.ErrorMessage =
                null;

            await _db.SaveChangesAsync(
                cancellationToken);

            result.Synced++;

            result.Messages.Add(
                $"Stock adjustment for '{movement.ProductName}' " +
                "synchronized.");
        }

        private async Task RecoverInterruptedItemsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            var interrupted =
                await _db.SyncQueueItems
                    .Where(item =>
                        item.TenantId == tenantId &&
                        item.EntityName ==
                            StockMovementEntityName &&
                        item.Status ==
                            SyncQueueStatus.Processing)
                    .ToListAsync(
                        cancellationToken);

            foreach (var item in interrupted)
            {
                item.Status =
                    SyncQueueStatus.Pending;

                item.ErrorMessage =
                    "Recovered after an interrupted synchronization.";
            }

            if (interrupted.Count > 0)
            {
                await _db.SaveChangesAsync(
                    cancellationToken);
            }
        }

        private static string BuildServerNotes(
            LocalStockMovement movement)
        {
            var value =
                $"Offline adjustment | " +
                $"ClientOperationId: {movement.ClientOperationId} | " +
                $"Before: {movement.QuantityBefore:0.###} | " +
                $"After: {movement.QuantityAfter:0.###}";

            if (!string.IsNullOrWhiteSpace(
                    movement.Notes))
            {
                value +=
                    $" | {movement.Notes.Trim()}";
            }

            return value.Length <= 500
                ? value
                : value[..500];
        }

        private static void TryAssignServerId(
            LocalStockMovement movement,
            object? serverMovement)
        {
            if (serverMovement == null)
            {
                return;
            }

            var property =
                serverMovement
                    .GetType()
                    .GetProperty("Id");

            if (property?.GetValue(
                    serverMovement) is Guid serverId &&
                serverId != Guid.Empty)
            {
                movement.ServerId =
                    serverId;
            }
        }

        private static bool IsAuthenticationError(
            ApiException exception)
        {
            return exception.StatusCode ==
                       System.Net.HttpStatusCode.Unauthorized ||
                   exception.StatusCode ==
                       System.Net.HttpStatusCode.Forbidden;
        }

        private static bool IsTemporaryError(
            ApiException exception)
        {
            var statusCode =
                (int)exception.StatusCode;

            return statusCode >= 500 ||
                   exception.StatusCode ==
                       System.Net.HttpStatusCode.RequestTimeout ||
                   exception.StatusCode ==
                       System.Net.HttpStatusCode.TooManyRequests;
        }

        private static string Truncate(
            string value,
            int maximumLength)
        {
            if (string.IsNullOrEmpty(
                    value))
            {
                return string.Empty;
            }

            return value.Length <=
                   maximumLength
                ? value
                : value[..maximumLength];
        }
    }
}
