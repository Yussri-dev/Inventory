using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Damages.Requests;
using Inventory.Dto.Enums;
using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.Infrastructure.Data;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inventory.Services.Handlers;

public sealed class DamageSyncBatchOperationHandler
    : ISyncBatchOperationHandler
{
    private const string CreateOperation =
        "Create";

    private const string DamageDocumentType =
        "41370";

    private const int MaximumErrorLength =
        2_000;

    private static readonly JsonSerializerOptions
        SerializerOptions =
            CreateSerializerOptions();

    private readonly InventoryDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IDocumentNumberService
        _documentNumberService;

    public DamageSyncBatchOperationHandler(
        InventoryDbContext db,
        ITenantContext tenantContext,
        IDocumentNumberService documentNumberService)
    {
        _db = db;
        _tenantContext = tenantContext;
        _documentNumberService = documentNumberService;
    }

    public string EntityName =>
        "Damage";

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

        var userId =
            _tenantContext.UserId;

        var parsedOperations =
            new List<ParsedDamageOperation>(
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

        var preparedOperations =
            new List<PreparedDamageOperation>(
                parsedOperations.Count);

        var simulatedQuantities =
            new Dictionary<Guid, decimal>();

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
                        $"Stock for Product '{request.ProductId}' " +
                        "was not found.");

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

            if (stock.Product == null ||
                stock.Product.IsDeleted)
            {
                results[operation.QueueItemId] =
                    Conflict(
                        operation,
                        $"Product '{request.ProductId}' was not found.");

                continue;
            }

            var quantity =
                RoundQuantity(
                    request.Quantity);

            if (!simulatedQuantities.TryGetValue(
                    request.ProductId,
                    out var quantityBefore))
            {
                quantityBefore =
                    RoundQuantity(
                        stock.Quantity);
            }

            var quantityAfter =
                RoundQuantity(
                    quantityBefore -
                    quantity);

            if (quantityAfter < 0m)
            {
                results[operation.QueueItemId] =
                    Conflict(
                        operation,
                        $"Damage quantity exceeds stock for Product " +
                        $"'{request.ProductId}'.");

                continue;
            }

            if (quantityAfter <
                stock.ReservedQuantity)
            {
                results[operation.QueueItemId] =
                    Conflict(
                        operation,
                        $"Damage quantity would reduce stock below " +
                        $"reserved quantity " +
                        $"({stock.ReservedQuantity:0.###}).");

                continue;
            }

            simulatedQuantities[request.ProductId] =
                quantityAfter;

            preparedOperations.Add(
                new PreparedDamageOperation
                {
                    ParsedOperation =
                        parsedOperation,

                    Stock =
                        stock,

                    Quantity =
                        quantity,

                    QuantityBefore =
                        quantityBefore,

                    QuantityAfter =
                        quantityAfter
                });
        }

        if (preparedOperations.Count == 0)
        {
            return OrderResults(
                operations,
                results);
        }

        var damageNumbers =
            await _documentNumberService
                .GenerateBatchTrackedAsync(
                    DamageDocumentType,
                    preparedOperations.Count);

        var now =
            DateTime.UtcNow;

        for (var index = 0;
             index < preparedOperations.Count;
             index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var preparedOperation =
                preparedOperations[index];

            var parsedOperation =
                preparedOperation.ParsedOperation;

            var operation =
                parsedOperation.Operation;

            var request =
                parsedOperation.Request;

            var stock =
                preparedOperation.Stock;

            var damage =
                new Damage
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    ProductId =
                        request.ProductId,

                    DamageNumber =
                        damageNumbers[index],

                    Quantity =
                        preparedOperation.Quantity,

                    EstimatedValue =
                        RoundMoney(
                            request.EstimatedValue),

                    Reason =
                        request.Reason?.Trim() ??
                        string.Empty,

                    Category =
                        Normalize(
                            request.Category,
                            100),

                    DamageDate =
                        now,

                    Status =
                        DamageStatus.Validated,

                    ValidatedAt =
                        now,

                    ValidatedByUserId =
                        userId,

                    CreatedAt =
                        now,

                    CreatedByUserId =
                        userId,

                    ModifiedAt =
                        now,

                    ModifiedByUserId =
                        userId,

                    IsDeleted =
                        false
                };

            stock.Quantity =
                preparedOperation.QuantityAfter;

            stock.LastUpdated =
                now;

            stock.ModifiedAt =
                now;

            stock.ModifiedByUserId =
                userId;

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
                        -preparedOperation.Quantity,

                    QuantityBefore =
                        preparedOperation.QuantityBefore,

                    QuantityAfter =
                        preparedOperation.QuantityAfter,

                    Type =
                        StockMovementType.Damage,

                    UnitCost =
                        RoundMoney(
                            stock.Product.PurchasePrice),

                    ReferenceId =
                        damage.Id,

                    ReferenceNumber =
                        damage.DamageNumber,

                    Notes =
                        BuildMovementNotes(
                            damage),

                    MovementDate =
                        now,

                    CreatedAt =
                        now,

                    CreatedByUserId =
                        userId,

                    ModifiedAt =
                        now,

                    ModifiedByUserId =
                        userId,

                    IsDeleted =
                        false
                };

            _db.Damages.Add(
                damage);

            _db.StockMovements.Add(
                movement);

            results[operation.QueueItemId] =
                Done(
                    operation,
                    damage.Id);
        }

        /*
         * Do not call SaveChangesAsync and do not open a transaction.
         * SyncBatchOperationExecutor owns both.
         */
        return OrderResults(
            operations,
            results);
    }

    private static ParsedDamageOperation?
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
                    $"Unsupported Damage operation " +
                    $"'{operation.Operation}'.";

                return null;
            }

            if (operation.ClientOperationId == Guid.Empty)
            {
                errorMessage =
                    "ClientOperationId is required for Damage Create.";

                return null;
            }

            var request =
                DeserializePayload<CreateDamageRequest>(
                    operation);

            if (request.ProductId == Guid.Empty)
            {
                errorMessage =
                    "ProductId is required for Damage Create.";

                return null;
            }

            var quantity =
                RoundQuantity(
                    request.Quantity);

            if (quantity <= 0m)
            {
                errorMessage =
                    "Damage quantity must be greater than zero.";

                return null;
            }

            if (request.EstimatedValue < 0m)
            {
                errorMessage =
                    "EstimatedValue cannot be negative.";

                return null;
            }

            if (request.Reason?.Length > 1_000)
            {
                errorMessage =
                    "Reason cannot exceed 1000 characters.";

                return null;
            }

            if (request.Category?.Length > 100)
            {
                errorMessage =
                    "Category cannot exceed 100 characters.";

                return null;
            }

            return new ParsedDamageOperation
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
                    $"Invalid Damage payload: " +
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
                        "No Damage synchronization result " +
                        "was produced."))
            .ToArray();
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

    private static string BuildMovementNotes(
        Damage damage)
    {
        var value =
            string.IsNullOrWhiteSpace(
                damage.Reason)
                ? $"Damage {damage.DamageNumber}"
                : $"Damage {damage.DamageNumber}: " +
                  damage.Reason.Trim();

        return value.Length <= 500
            ? value
            : value[..500];
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

    private sealed class ParsedDamageOperation
    {
        public required SyncBatchOperationRequest Operation
        {
            get;
            init;
        }

        public required CreateDamageRequest Request
        {
            get;
            init;
        }
    }

    private sealed class PreparedDamageOperation
    {
        public required ParsedDamageOperation ParsedOperation
        {
            get;
            init;
        }

        public required Stock Stock
        {
            get;
            init;
        }

        public decimal Quantity
        {
            get;
            init;
        }

        public decimal QuantityBefore
        {
            get;
            init;
        }

        public decimal QuantityAfter
        {
            get;
            init;
        }
    }
}