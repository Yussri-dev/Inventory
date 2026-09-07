using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.Infrastructure.Data;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inventory.Services.Handlers
{
    public sealed class SupplierSyncBatchOperationHandler
       : ISyncBatchOperationHandler
    {
        private const string CreateOperation =
            "Create";

        private const string UpdateOperation =
            "Update";

        private const string DeleteOperation =
            "Delete";

        private const int MaximumErrorLength =
            2_000;

        private static readonly JsonSerializerOptions
            SerializerOptions =
                CreateSerializerOptions();

        private readonly InventoryDbContext _db;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public SupplierSyncBatchOperationHandler(
            InventoryDbContext db,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _db =
                db;

            _mapper =
                mapper;

            _tenantContext =
                tenantContext;
        }

        public string EntityName =>
            "Supplier";

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
                new List<ParsedSupplierOperation>(
                    operations.Count);

            var results =
                new Dictionary<Guid, SyncBatchItemResult>();

            foreach (var operation in operations)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var parsedOperation =
                    TryParseOperation(
                        operation,
                        out var parseError);

                if (parsedOperation == null)
                {
                    results[operation.QueueItemId] =
                        Conflict(
                            operation,
                            parseError);

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

            var targetSupplierIds =
                parsedOperations
                    .Where(parsed =>
                        parsed.Kind == SupplierOperationKind.Update ||
                        parsed.Kind == SupplierOperationKind.Delete)
                    .Select(parsed =>
                        parsed.ServerEntityId)
                    .Where(id =>
                        id != Guid.Empty)
                    .Distinct()
                    .ToArray();

            var suppliersById =
                targetSupplierIds.Length == 0
                    ? new Dictionary<Guid, Supplier>()
                    : await _db.Suppliers
                        .Where(supplier =>
                            supplier.TenantId == tenantId &&
                            targetSupplierIds.Contains(
                                supplier.Id))
                        .ToDictionaryAsync(
                            supplier =>
                                supplier.Id,
                            cancellationToken);

            var requestedNames =
                parsedOperations
                    .Select(GetRequestedName)
                    .Where(name =>
                        !string.IsNullOrWhiteSpace(
                            name))
                    .Select(name =>
                        name!)
                    .Distinct(
                        StringComparer.Ordinal)
                    .ToArray();

            var existingNameOwners =
                requestedNames.Length == 0
                    ? new List<SupplierNameOwner>()
                    : await _db.Suppliers
                        .AsNoTracking()
                        .Where(supplier =>
                            supplier.TenantId == tenantId &&
                            !supplier.IsDeleted &&
                            requestedNames.Contains(
                                supplier.Name))
                        .Select(supplier =>
                            new SupplierNameOwner
                            {
                                SupplierId =
                                    supplier.Id,

                                Name =
                                    supplier.Name
                            })
                        .ToListAsync(
                            cancellationToken);

            var nameOwners =
                new Dictionary<string, HashSet<Guid>>(
                    StringComparer.Ordinal);

            foreach (var owner in existingNameOwners)
            {
                AddNameOwner(
                    nameOwners,
                    owner.Name,
                    owner.SupplierId);
            }

            /*
             * Add the current names of all loaded targets. This permits
             * ordered operations such as Delete B followed by Rename A to B.
             */
            foreach (var supplier in suppliersById.Values)
            {
                if (!supplier.IsDeleted &&
                    !string.IsNullOrWhiteSpace(
                        supplier.Name))
                {
                    AddNameOwner(
                        nameOwners,
                        supplier.Name,
                        supplier.Id);
                }
            }

            foreach (var parsedOperation in parsedOperations)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var operation =
                    parsedOperation.Operation;

                SyncBatchItemResult itemResult;

                switch (parsedOperation.Kind)
                {
                    case SupplierOperationKind.Create:
                        itemResult =
                            ProcessCreate(
                                parsedOperation,
                                tenantId,
                                nameOwners,
                                suppliersById);
                        break;

                    case SupplierOperationKind.Update:
                        itemResult =
                            ProcessUpdate(
                                parsedOperation,
                                nameOwners,
                                suppliersById);
                        break;

                    case SupplierOperationKind.Delete:
                        itemResult =
                            ProcessDelete(
                                parsedOperation,
                                nameOwners,
                                suppliersById);
                        break;

                    default:
                        itemResult =
                            Conflict(
                                operation,
                                $"Unsupported Supplier operation " +
                                $"'{operation.Operation}'.");
                        break;
                }

                results[operation.QueueItemId] =
                    itemResult;
            }

            /*
             * Do not call SaveChanges and do not open a transaction here.
             * SyncBatchOperationExecutor owns both operations.
             */
            return OrderResults(
                operations,
                results);
        }

        private SyncBatchItemResult ProcessCreate(
            ParsedSupplierOperation parsedOperation,
            Guid tenantId,
            IDictionary<string, HashSet<Guid>> nameOwners,
            IDictionary<Guid, Supplier> suppliersById)
        {
            var operation =
                parsedOperation.Operation;

            var request =
                parsedOperation.CreateRequest!;

            if (string.IsNullOrWhiteSpace(
                    request.Name))
            {
                return Conflict(
                    operation,
                    "Supplier name must not be empty.");
            }

            if (IsNameOwned(
                    nameOwners,
                    request.Name))
            {
                return Conflict(
                    operation,
                    $"Supplier with name '{request.Name}' " +
                    "already exists.");
            }

            var now =
                DateTime.UtcNow;

            var supplier =
                _mapper.Map<Supplier>(
                    request);

            supplier.Id =
                Guid.NewGuid();

            supplier.TenantId =
                tenantId;

            supplier.CreatedAt =
                now;

            supplier.ModifiedAt =
                now;

            /*
             * Preserve the existing SupplierService create semantics.
             */
            supplier.IsActive =
                true;

            supplier.IsDeleted =
                false;

            _db.Suppliers.Add(
                supplier);

            suppliersById[supplier.Id] =
                supplier;

            AddNameOwner(
                nameOwners,
                supplier.Name,
                supplier.Id);

            return Done(
                operation,
                supplier.Id);
        }

        private SyncBatchItemResult ProcessUpdate(
            ParsedSupplierOperation parsedOperation,
            IDictionary<string, HashSet<Guid>> nameOwners,
            IReadOnlyDictionary<Guid, Supplier> suppliersById)
        {
            var operation =
                parsedOperation.Operation;

            var request =
                parsedOperation.UpdateRequest!;

            var serverEntityId =
                parsedOperation.ServerEntityId;

            if (!suppliersById.TryGetValue(
                    serverEntityId,
                    out var supplier) ||
                supplier.IsDeleted)
            {
                return Conflict(
                    operation,
                    $"Supplier '{serverEntityId}' was not found.");
            }

            var previousName =
                supplier.Name;

            var nameChanged =
                !string.Equals(
                    request.Name,
                    previousName,
                    StringComparison.Ordinal);

            if (!string.IsNullOrWhiteSpace(
                    request.Name) &&
                nameChanged &&
                IsNameOwnedByAnotherSupplier(
                    nameOwners,
                    request.Name,
                    serverEntityId))
            {
                return Conflict(
                    operation,
                    $"Supplier with name '{request.Name}' " +
                    "already exists.");
            }

            request.Id =
                serverEntityId;

            _mapper.Map(
                request,
                supplier);

            supplier.ModifiedAt =
                DateTime.UtcNow;

            if (nameChanged)
            {
                RemoveNameOwner(
                    nameOwners,
                    previousName,
                    serverEntityId);

                if (!string.IsNullOrWhiteSpace(
                        supplier.Name))
                {
                    AddNameOwner(
                        nameOwners,
                        supplier.Name,
                        serverEntityId);
                }
            }

            return Done(
                operation,
                serverEntityId);
        }

        private static SyncBatchItemResult ProcessDelete(
            ParsedSupplierOperation parsedOperation,
            IDictionary<string, HashSet<Guid>> nameOwners,
            IReadOnlyDictionary<Guid, Supplier> suppliersById)
        {
            var operation =
                parsedOperation.Operation;

            var serverEntityId =
                parsedOperation.ServerEntityId;

            if (!suppliersById.TryGetValue(
                    serverEntityId,
                    out var supplier) ||
                supplier.IsDeleted)
            {
                return Conflict(
                    operation,
                    $"Supplier '{serverEntityId}' was not found.");
            }

            RemoveNameOwner(
                nameOwners,
                supplier.Name,
                supplier.Id);

            supplier.IsDeleted =
                true;

            supplier.ModifiedAt =
                DateTime.UtcNow;

            return Done(
                operation,
                serverEntityId);
        }

        private static ParsedSupplierOperation?
            TryParseOperation(
                SyncBatchOperationRequest operation,
                out string? errorMessage)
        {
            errorMessage =
                null;

            try
            {
                if (IsOperation(
                        operation,
                        CreateOperation))
                {
                    return new ParsedSupplierOperation
                    {
                        Operation =
                            operation,

                        Kind =
                            SupplierOperationKind.Create,

                        CreateRequest =
                            DeserializePayload<CreateSupplierRequest>(
                                operation)
                    };
                }

                if (IsOperation(
                        operation,
                        UpdateOperation))
                {
                    var request =
                        DeserializePayload<UpdateSupplierRequest>(
                            operation);

                    var serverEntityId =
                        GetServerEntityId(
                            operation,
                            request.Id);

                    if (serverEntityId == Guid.Empty)
                    {
                        errorMessage =
                            "ServerEntityId is required for Supplier " +
                            "Update.";

                        return null;
                    }

                    return new ParsedSupplierOperation
                    {
                        Operation =
                            operation,

                        Kind =
                            SupplierOperationKind.Update,

                        ServerEntityId =
                            serverEntityId,

                        UpdateRequest =
                            request
                    };
                }

                if (IsOperation(
                        operation,
                        DeleteOperation))
                {
                    var serverEntityId =
                        GetServerEntityId(
                            operation);

                    if (serverEntityId == Guid.Empty)
                    {
                        errorMessage =
                            "ServerEntityId is required for Supplier " +
                            "Delete.";

                        return null;
                    }

                    return new ParsedSupplierOperation
                    {
                        Operation =
                            operation,

                        Kind =
                            SupplierOperationKind.Delete,

                        ServerEntityId =
                            serverEntityId
                    };
                }

                errorMessage =
                    $"Unsupported Supplier operation " +
                    $"'{operation.Operation}'.";

                return null;
            }
            catch (JsonException exception)
            {
                errorMessage =
                    Truncate(
                        $"Invalid Supplier payload: " +
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

        private static string? GetRequestedName(
            ParsedSupplierOperation parsedOperation)
        {
            return parsedOperation.Kind switch
            {
                SupplierOperationKind.Create =>
                    parsedOperation.CreateRequest?.Name,

                SupplierOperationKind.Update =>
                    parsedOperation.UpdateRequest?.Name,

                _ =>
                    null
            };
        }

        private static Guid GetServerEntityId(
            SyncBatchOperationRequest operation,
            Guid requestId = default)
        {
            if (operation.ServerEntityId.HasValue &&
                operation.ServerEntityId.Value != Guid.Empty)
            {
                return operation.ServerEntityId.Value;
            }

            return requestId;
        }

        private static void AddNameOwner(
            IDictionary<string, HashSet<Guid>> nameOwners,
            string name,
            Guid supplierId)
        {
            if (string.IsNullOrWhiteSpace(
                    name))
            {
                return;
            }

            if (!nameOwners.TryGetValue(
                    name,
                    out var owners))
            {
                owners =
                    new HashSet<Guid>();

                nameOwners[name] =
                    owners;
            }

            owners.Add(
                supplierId);
        }

        private static void RemoveNameOwner(
            IDictionary<string, HashSet<Guid>> nameOwners,
            string name,
            Guid supplierId)
        {
            if (string.IsNullOrWhiteSpace(
                    name) ||
                !nameOwners.TryGetValue(
                    name,
                    out var owners))
            {
                return;
            }

            owners.Remove(
                supplierId);

            if (owners.Count == 0)
            {
                nameOwners.Remove(
                    name);
            }
        }

        private static bool IsNameOwned(
            IDictionary<string, HashSet<Guid>> nameOwners,
            string name)
        {
            return nameOwners.TryGetValue(
                       name,
                       out var owners) &&
                   owners.Count > 0;
        }

        private static bool IsNameOwnedByAnotherSupplier(
            IDictionary<string, HashSet<Guid>> nameOwners,
            string name,
            Guid supplierId)
        {
            return nameOwners.TryGetValue(
                       name,
                       out var owners) &&
                   owners.Any(ownerId =>
                       ownerId != supplierId);
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
                            "No Supplier synchronization result " +
                            "was produced."))
                .ToArray();
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

        private enum SupplierOperationKind
        {
            Create,
            Update,
            Delete
        }

        private sealed class ParsedSupplierOperation
        {
            public required SyncBatchOperationRequest Operation
            {
                get;
                init;
            }

            public required SupplierOperationKind Kind
            {
                get;
                init;
            }

            public Guid ServerEntityId
            {
                get;
                init;
            }

            public CreateSupplierRequest? CreateRequest
            {
                get;
                init;
            }

            public UpdateSupplierRequest? UpdateRequest
            {
                get;
                init;
            }
        }

        private sealed class SupplierNameOwner
        {
            public Guid SupplierId { get; set; }

            public string Name { get; set; } =
                string.Empty;
        }
    }
}