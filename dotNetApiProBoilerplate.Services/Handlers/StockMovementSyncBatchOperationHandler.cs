using Inventory.Domain.Entities;
using Inventory.Dto.Enums;
using Inventory.Dto.StockMouvements.Requests;
using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.Infrastructure.Data;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inventory.Services.Handlers;

public sealed class StockMovementSyncBatchOperationHandler
    : ISyncBatchOperationHandler
{
    private const string CreateOperation =
        "Create";

    private const int MaximumErrorLength =
        2_000;

    private static readonly JsonSerializerOptions
        SerializerOptions =
            CreateSerializerOptions();

    private readonly InventoryDbContext _db;
    private readonly ITenantContext _tenantContext;

    public StockMovementSyncBatchOperationHandler(
        InventoryDbContext db,
        ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public string EntityName =>
        "StockMovement";

    public async Task<IReadOnlyList<SyncBatchItemResult>>
        ProcessBatchAsync(
            IReadOnlyList<SyncBatchOperationRequest> operations,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            operations);

        if (operations.Count == 0)
        {
            return Array.Empty<SyncBatchItemResult>();
        }

        var tenantId =
            _tenantContext.TenantId;

        var parsedOperations =
            new List<ParsedStockMovementOperation>(
                operations.Count);

        var results =
            new Dictionary<Guid, SyncBatchItemResult>();

        foreach (var operation in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parsedOperation =
                TryParseOperation(
                    operation,
                    out var errorMessage);

            if (parsedOperation == null)
            {
                results[operation.QueueItemId] =
                    Conflict(
                        operation,
                        errorMessage);

                continue;
            }

            parsedOperations.Add(
                parsedOperation);
        }

        if (parsedOperations.Count == 0)
        {
            return OrderResults(
                operations,
                results);
        }

        var productIds =
            parsedOperations
                .Select(item =>
                    item.Request.ProductId)
                .Distinct()
                .ToArray();

        var stocks =
            await _db.Stocks
                .Include(stock =>
                    stock.Product)
                .Where(stock =>
                    stock.TenantId == tenantId &&
                    !stock.IsDeleted &&
                    productIds.Contains(
                        stock.ProductId))
                .ToListAsync(
                    cancellationToken);

        var stocksByProductId =
            stocks
                .GroupBy(stock =>
                    stock.ProductId)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group.ToArray());

        foreach (var parsedOperation in parsedOperations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var operation =
                parsedOperation.Operation;

            var request =
                parsedOperation.Request;

            if (!stocksByProductId.TryGetValue(
                    request.ProductId,
                    out var productStocks) ||
                productStocks.Length == 0)
            {
                results[operation.QueueItemId] =
                    Conflict(
                        operation,
                        $"Stock for Product " +
                        $"'{request.ProductId}' was not found.");

                continue;
            }

            if (productStocks.Length > 1)
            {
                results[operation.QueueItemId] =
                    Conflict(
                        operation,
                        $"Multiple stock rows exist for Product " +
                        $"'{request.ProductId}'.");

                continue;
            }

            var stock =
                productStocks[0];

            var quantityChange =
                RoundQuantity(
                    request.QuantityChange);

            var quantityBefore =
                RoundQuantity(
                    stock.Quantity);

            var quantityAfter =
                RoundQuantity(
                    quantityBefore +
                    quantityChange);

            if (quantityAfter < 0m)
            {
                results[operation.QueueItemId] =
                    Conflict(
                        operation,
                        "Resulting stock quantity cannot be negative.");

                continue;
            }

            if (quantityAfter <
                stock.ReservedQuantity)
            {
                results[operation.QueueItemId] =
                    Conflict(
                        operation,
                        $"Resulting stock quantity cannot be lower " +
                        $"than reserved quantity " +
                        $"({stock.ReservedQuantity:0.###}).");

                continue;
            }

            var now =
                DateTime.UtcNow;

            var movement =
                new StockMovement
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    ProductId =
                        request.ProductId,

                    QuantityChange =
                        quantityChange,

                    QuantityBefore =
                        quantityBefore,

                    QuantityAfter =
                        quantityAfter,

                    Type =
                        StockMovementType.Adjustment,

                    UnitCost =
                        RoundMoney(
                            stock.Product.PurchasePrice),

                    ReferenceId =
                        request.ReferenceId,

                    ReferenceNumber =
                        Normalize(
                            request.ReferenceNumber,
                            200),

                    Notes =
                        Normalize(
                            request.Notes,
                            500),

                    /*
                     * Preserve the current StockMouvementService
                     * semantics: the server determines the date.
                     */
                    MovementDate =
                        now,

                    CreatedAt =
                        now,

                    ModifiedAt =
                        now,

                    IsDeleted =
                        false
                };

            stock.Quantity =
                quantityAfter;

            stock.LastUpdated =
                now;

            stock.ModifiedAt =
                now;

            _db.StockMovements.Add(
                movement);

            results[operation.QueueItemId] =
                Done(
                    operation,
                    movement.Id);
        }

        /*
         * SyncBatchOperationExecutor owns the transaction
         * and SaveChangesAsync.
         */
        return OrderResults(
            operations,
            results);
    }

    private static ParsedStockMovementOperation?
        TryParseOperation(
            SyncBatchOperationRequest operation,
            out string? errorMessage)
    {
        errorMessage =
            null;

        try
        {
            if (!IsOperation(
                    operation,
                    CreateOperation))
            {
                errorMessage =
                    $"Unsupported StockMovement operation " +
                    $"'{operation.Operation}'.";

                return null;
            }

            var request =
                DeserializePayload<CreateStockMouvementRequest>(
                    operation);

            if (request.ProductId == Guid.Empty)
            {
                errorMessage =
                    "ProductId is required for StockMovement Create.";

                return null;
            }

            if (request.QuantityChange == 0m)
            {
                errorMessage =
                    "QuantityChange cannot be zero.";

                return null;
            }

            if (request.Type !=
                StockMovementType.Adjustment)
            {
                errorMessage =
                    "Only Adjustment stock movements can be " +
                    "synchronized directly.";

                return null;
            }

            if (request.ReferenceNumber?.Length > 200)
            {
                errorMessage =
                    "ReferenceNumber cannot exceed 200 characters.";

                return null;
            }

            if (request.Notes?.Length > 500)
            {
                errorMessage =
                    "Notes cannot exceed 500 characters.";

                return null;
            }

            return new ParsedStockMovementOperation
            {
                Operation =
                    operation,

                Request =
                    request
            };
        }
        catch (JsonException exception)
        {
            errorMessage =
                Truncate(
                    $"Invalid StockMovement payload: " +
                    exception.Message);

            return null;
        }
    }

    private static TRequest DeserializePayload<TRequest>(
        SyncBatchOperationRequest operation)
    {
        var request =
            operation.Payload.Deserialize<TRequest>(
                SerializerOptions);

        if (request == null)
        {
            throw new JsonException(
                $"Payload could not be converted to " +
                $"'{typeof(TRequest).Name}'.");
        }

        return request;
    }

    private static bool IsOperation(
        SyncBatchOperationRequest operation,
        string expectedOperation)
    {
        return string.Equals(
            operation.Operation?.Trim(),
            expectedOperation,
            StringComparison.OrdinalIgnoreCase);
    }

    private static SyncBatchItemResult Done(
        SyncBatchOperationRequest operation,
        Guid serverEntityId)
    {
        return new SyncBatchItemResult
        {
            QueueItemId =
                operation.QueueItemId,

            ClientOperationId =
                operation.ClientOperationId,

            ServerEntityId =
                serverEntityId,

            Status =
                SyncBatchItemStatus.Done,

            ErrorMessage =
                null
        };
    }

    private static SyncBatchItemResult Conflict(
        SyncBatchOperationRequest operation,
        string? errorMessage)
    {
        return new SyncBatchItemResult
        {
            QueueItemId =
                operation.QueueItemId,

            ClientOperationId =
                operation.ClientOperationId,

            ServerEntityId =
                operation.ServerEntityId,

            Status =
                SyncBatchItemStatus.Conflict,

            ErrorMessage =
                Truncate(
                    errorMessage)
        };
    }

    private static IReadOnlyList<SyncBatchItemResult>
        OrderResults(
            IReadOnlyList<SyncBatchOperationRequest> operations,
            IReadOnlyDictionary<Guid, SyncBatchItemResult> results)
    {
        return operations
            .Select(operation =>
                results.TryGetValue(
                    operation.QueueItemId,
                    out var result)
                    ? result
                    : Conflict(
                        operation,
                        "No StockMovement synchronization " +
                        "result was produced."))
            .ToArray();
    }

    private static decimal RoundQuantity(
        decimal value)
    {
        return Math.Round(
            value,
            3,
            MidpointRounding.AwayFromZero);
    }

    private static decimal RoundMoney(
        decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static string? Normalize(
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        value =
            value.Trim();

        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }

    private static JsonSerializerOptions
        CreateSerializerOptions()
    {
        var options =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive =
                    true
            };

        options.Converters.Add(
            new JsonStringEnumConverter());

        return options;
    }

    private static string? Truncate(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        value =
            value.Trim();

        return value.Length <= MaximumErrorLength
            ? value
            : value[..MaximumErrorLength];
    }

    private sealed class ParsedStockMovementOperation
    {
        public required SyncBatchOperationRequest Operation
        {
            get;
            init;
        }

        public required CreateStockMouvementRequest Request
        {
            get;
            init;
        }
    }
}