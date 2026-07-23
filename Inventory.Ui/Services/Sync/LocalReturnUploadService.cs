using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Refit;

namespace Inventory.Ui.Services.Sync;

public sealed class LocalReturnUploadService
    : ILocalReturnUploadService
{
    private const string ReturnEntityName =
        "Return";

    private readonly PosLocalDbContext _db;
    private readonly IReturnApi _returnApi;
    private readonly ILocalTenantContext _tenantContext;
    private readonly ILogger<LocalReturnUploadService>
        _logger;

    public LocalReturnUploadService(
        PosLocalDbContext db,
        IReturnApi returnApi,
        ILocalTenantContext tenantContext,
        ILogger<LocalReturnUploadService> logger)
    {
        _db = db;
        _returnApi = returnApi;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<LocalReturnUploadResult>
        SyncPendingAsync(
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var result =
            new LocalReturnUploadResult();

        await RecoverInterruptedItemsAsync(
            tenantId,
            cancellationToken);

        var queueItems =
            await _db.SyncQueueItems
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.EntityName ==
                        ReturnEntityName &&
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
            cancellationToken
                .ThrowIfCancellationRequested();

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
                    "API offline. The return remains queued.";

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

                queueItem.Status =
                    IsTemporaryError(
                        exception) ||
                    IsAuthenticationError(
                        exception)
                        ? SyncQueueStatus.Pending
                        : SyncQueueStatus.Conflict;

                result.Failed++;

                result.Messages.Add(
                    $"Return upload failed: " +
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
                    $"Return upload failed: " +
                    $"{queueItem.ErrorMessage}");

                _logger.LogError(
                    exception,
                    "Failed to upload local return {QueueId}.",
                    queueItem.Id);

                await _db.SaveChangesAsync(
                    cancellationToken);
            }
        }

        return result;
    }

    private async Task UploadOneAsync(
        SyncQueueItem queueItem,
        LocalReturnUploadResult result,
        CancellationToken cancellationToken)
    {
        queueItem.Status =
            SyncQueueStatus.Processing;

        queueItem.Attempts++;

        queueItem.ErrorMessage =
            null;

        await _db.SaveChangesAsync(
            cancellationToken);

        var localReturn =
            await _db.Returns
                .Include(item =>
                    item.Lines)
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId ==
                            queueItem.TenantId &&
                        item.Id ==
                            queueItem.LocalEntityId,
                    cancellationToken);

        if (localReturn ==
            null)
        {
            queueItem.Status =
                SyncQueueStatus.Failed;

            queueItem.ErrorMessage =
                "The local return was not found.";

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
                $"Unsupported return operation: " +
                $"{queueItem.Operation}";

            localReturn.SyncStatus =
                SyncQueueStatus.Failed;

            result.Failed++;

            result.Messages.Add(
                queueItem.ErrorMessage);

            await _db.SaveChangesAsync(
                cancellationToken);

            return;
        }

        if (localReturn.ServerId.HasValue &&
            localReturn.ServerId.Value !=
                Guid.Empty)
        {
            MarkDone(
                localReturn,
                queueItem,
                DateTime.UtcNow);

            result.Skipped++;

            result.Messages.Add(
                $"Return {localReturn.LocalReturnNumber} " +
                "already has a ServerId.");

            await _db.SaveChangesAsync(
                cancellationToken);

            return;
        }

        await RefreshDependenciesAsync(
            localReturn,
            cancellationToken);

        if (!localReturn.ServerSaleId.HasValue ||
            localReturn.ServerSaleId.Value ==
                Guid.Empty)
        {
            MarkDependencyPending(
                localReturn,
                queueItem,
                "The original sale must be synchronized first.");

            result.Skipped++;

            result.Messages.Add(
                queueItem.ErrorMessage!);

            await _db.SaveChangesAsync(
                cancellationToken);

            return;
        }

        var missingProduct =
            localReturn.Lines
                .FirstOrDefault(line =>
                    !line.ProductServerId.HasValue ||
                    line.ProductServerId.Value ==
                        Guid.Empty);

        if (missingProduct !=
            null)
        {
            MarkDependencyPending(
                localReturn,
                queueItem,
                $"Product '{missingProduct.ProductName}' must be " +
                "synchronized first.");

            result.Skipped++;

            result.Messages.Add(
                queueItem.ErrorMessage!);

            await _db.SaveChangesAsync(
                cancellationToken);

            return;
        }

        var request =
            LocalReturnSyncMapper
                .ToCreateCompleteReturnRequest(
                    localReturn);

        var serverReturn =
            await _returnApi.CreateComplete(
                request,
                cancellationToken);

        var now =
            DateTime.UtcNow;

        localReturn.ServerId =
            serverReturn.Id;

        localReturn.ServerReturnNumber =
            serverReturn.ReturnNumber;

        localReturn.LastSyncedAtUtc =
            now;

        localReturn.SyncStatus =
            SyncQueueStatus.Done;

        foreach (var line in localReturn.Lines)
        {
            /*
             * ReturnLineResult IDs are not returned by the current
             * complete endpoint, so only the header receives ServerId.
             */
            line.ProductServerId ??=
                await ResolveProductServerIdAsync(
                    line.ProductLocalId,
                    queueItem.TenantId,
                    cancellationToken);
        }

        MarkDone(
            localReturn,
            queueItem,
            now);

        await _db.SaveChangesAsync(
            cancellationToken);

        result.Synced++;

        result.Messages.Add(
            $"Return {localReturn.LocalReturnNumber} uploaded " +
            $"as {localReturn.ServerReturnNumber}.");
    }

    private async Task RefreshDependenciesAsync(
        LocalReturn localReturn,
        CancellationToken cancellationToken)
    {
        if (!localReturn.ServerSaleId.HasValue ||
            localReturn.ServerSaleId.Value ==
                Guid.Empty)
        {
            var sale =
                await _db.Sales
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId ==
                                localReturn.TenantId &&
                            item.Id ==
                                localReturn.LocalSaleId,
                        cancellationToken);

            localReturn.ServerSaleId =
                sale?.ServerId;

            localReturn.OriginalServerInvoiceNumber =
                sale?.ServerInvoiceNumber;

            localReturn.CustomerServerId =
                sale?.CustomerServerId;
        }

        foreach (var line in localReturn.Lines)
        {
            if (!line.ProductServerId.HasValue ||
                line.ProductServerId.Value ==
                    Guid.Empty)
            {
                line.ProductServerId =
                    await ResolveProductServerIdAsync(
                        line.ProductLocalId,
                        localReturn.TenantId,
                        cancellationToken);
            }

            if (!line.UnitProductServerId.HasValue ||
                line.UnitProductServerId.Value ==
                    Guid.Empty)
            {
                line.UnitProductServerId =
                    await ResolveProductServerIdAsync(
                        line.UnitProductLocalId,
                        localReturn.TenantId,
                        cancellationToken);
            }
        }
    }

    private async Task<Guid?> ResolveProductServerIdAsync(
        Guid localProductId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (localProductId ==
            Guid.Empty)
        {
            return null;
        }

        return await _db.Products
            .AsNoTracking()
            .Where(product =>
                product.TenantId == tenantId &&
                product.Id == localProductId &&
                !product.IsDeletedLocally)
            .Select(product =>
                product.ServerId)
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    private static void MarkDependencyPending(
        LocalReturn localReturn,
        SyncQueueItem queueItem,
        string message)
    {
        localReturn.SyncStatus =
            SyncQueueStatus.Pending;

        queueItem.Status =
            SyncQueueStatus.Pending;

        queueItem.ErrorMessage =
            message;
    }

    private static void MarkDone(
        LocalReturn localReturn,
        SyncQueueItem queueItem,
        DateTime now)
    {
        localReturn.SyncStatus =
            SyncQueueStatus.Done;

        localReturn.LastSyncedAtUtc =
            now;

        queueItem.Status =
            SyncQueueStatus.Done;

        queueItem.ProcessedAtUtc =
            now;

        queueItem.ErrorMessage =
            null;
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
                        ReturnEntityName &&
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

        if (interrupted.Count >
            0)
        {
            await _db.SaveChangesAsync(
                cancellationToken);
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

        return statusCode >=
                   500 ||
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
