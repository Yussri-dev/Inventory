using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Customers.Requests;
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
    public sealed class CustomerSyncBatchOperationHandler
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

        public CustomerSyncBatchOperationHandler(
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
            "Customer";

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
                new List<ParsedCustomerOperation>(
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

            var targetCustomerIds =
                parsedOperations
                    .Where(parsed =>
                        parsed.Kind == CustomerOperationKind.Update ||
                        parsed.Kind == CustomerOperationKind.Delete)
                    .Select(parsed =>
                        parsed.ServerEntityId)
                    .Where(id =>
                        id != Guid.Empty)
                    .Distinct()
                    .ToArray();

            var customersById =
                targetCustomerIds.Length == 0
                    ? new Dictionary<Guid, Customer>()
                    : await _db.Customers
                        .Where(customer =>
                            customer.TenantId == tenantId &&
                            targetCustomerIds.Contains(
                                customer.Id))
                        .ToDictionaryAsync(
                            customer =>
                                customer.Id,
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
                    ? new List<CustomerNameOwner>()
                    : await _db.Customers
                        .AsNoTracking()
                        .Where(customer =>
                            customer.TenantId == tenantId &&
                            !customer.IsDeleted &&
                            requestedNames.Contains(
                                customer.Name))
                        .Select(customer =>
                            new CustomerNameOwner
                            {
                                CustomerId =
                                    customer.Id,

                                Name =
                                    customer.Name
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
                    owner.CustomerId);
            }

            /*
             * Add the current names of all loaded targets. This permits
             * ordered operations such as Delete B followed by Rename A to B.
             */
            foreach (var customer in customersById.Values)
            {
                if (!customer.IsDeleted &&
                    !string.IsNullOrWhiteSpace(
                        customer.Name))
                {
                    AddNameOwner(
                        nameOwners,
                        customer.Name,
                        customer.Id);
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
                    case CustomerOperationKind.Create:
                        itemResult =
                            ProcessCreate(
                                parsedOperation,
                                tenantId,
                                userId,
                                nameOwners,
                                customersById);
                        break;

                    case CustomerOperationKind.Update:
                        itemResult =
                            ProcessUpdate(
                                parsedOperation,
                                userId,
                                nameOwners,
                                customersById);
                        break;

                    case CustomerOperationKind.Delete:
                        itemResult =
                            ProcessDelete(
                                parsedOperation,
                                userId,
                                nameOwners,
                                customersById);
                        break;

                    default:
                        itemResult =
                            Conflict(
                                operation,
                                $"Unsupported Customer operation " +
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
            ParsedCustomerOperation parsedOperation,
            Guid tenantId,
            Guid userId,
            IDictionary<string, HashSet<Guid>> nameOwners,
            IDictionary<Guid, Customer> customersById)
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
                    "Customer name must not be empty.");
            }

            if (IsNameOwned(
                    nameOwners,
                    request.Name))
            {
                return Conflict(
                    operation,
                    $"Customer with name '{request.Name}' " +
                    "already exists.");
            }

            var now =
                DateTime.UtcNow;

            var customer =
                _mapper.Map<Customer>(
                    request);

            customer.Id =
                Guid.NewGuid();

            customer.TenantId =
                tenantId;

            customer.CreatedByUserId =
                userId;

            customer.CreatedAt =
                now;

            /*
             * Preserve the existing CustomerService create semantics.
             */
            customer.IsActive =
                true;

            customer.IsDeleted =
                false;

            _db.Customers.Add(
                customer);

            customersById[customer.Id] =
                customer;

            AddNameOwner(
                nameOwners,
                customer.Name,
                customer.Id);

            return Done(
                operation,
                customer.Id);
        }

        private SyncBatchItemResult ProcessUpdate(
            ParsedCustomerOperation parsedOperation,
            Guid userId,
            IDictionary<string, HashSet<Guid>> nameOwners,
            IReadOnlyDictionary<Guid, Customer> customersById)
        {
            var operation =
                parsedOperation.Operation;

            var request =
                parsedOperation.UpdateRequest!;

            var serverEntityId =
                parsedOperation.ServerEntityId;

            if (!customersById.TryGetValue(
                    serverEntityId,
                    out var customer) ||
                customer.IsDeleted)
            {
                return Conflict(
                    operation,
                    $"Customer '{serverEntityId}' was not found.");
            }

            var previousName =
                customer.Name;

            var nameChanged =
                !string.Equals(
                    request.Name,
                    previousName,
                    StringComparison.Ordinal);

            if (!string.IsNullOrWhiteSpace(
                    request.Name) &&
                nameChanged &&
                IsNameOwnedByAnotherCustomer(
                    nameOwners,
                    request.Name,
                    serverEntityId))
            {
                return Conflict(
                    operation,
                    $"Customer with name '{request.Name}' " +
                    "already exists.");
            }

            request.Id =
                serverEntityId;

            _mapper.Map(
                request,
                customer);

            customer.ModifiedAt =
                DateTime.UtcNow;

            customer.ModifiedByUserId =
                userId;

            if (nameChanged)
            {
                RemoveNameOwner(
                    nameOwners,
                    previousName,
                    serverEntityId);

                if (!string.IsNullOrWhiteSpace(
                        customer.Name))
                {
                    AddNameOwner(
                        nameOwners,
                        customer.Name,
                        serverEntityId);
                }
            }

            return Done(
                operation,
                serverEntityId);
        }

        private static SyncBatchItemResult ProcessDelete(
            ParsedCustomerOperation parsedOperation,
            Guid userId,
            IDictionary<string, HashSet<Guid>> nameOwners,
            IReadOnlyDictionary<Guid, Customer> customersById)
        {
            var operation =
                parsedOperation.Operation;

            var serverEntityId =
                parsedOperation.ServerEntityId;

            if (!customersById.TryGetValue(
                    serverEntityId,
                    out var customer) ||
                customer.IsDeleted)
            {
                return Conflict(
                    operation,
                    $"Customer '{serverEntityId}' was not found.");
            }

            RemoveNameOwner(
                nameOwners,
                customer.Name,
                customer.Id);

            customer.IsDeleted =
                true;

            customer.DeletedAt =
                DateTime.UtcNow;

            customer.DeletedByUserId =
                userId;

            return Done(
                operation,
                serverEntityId);
        }

        private static ParsedCustomerOperation?
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
                    return new ParsedCustomerOperation
                    {
                        Operation =
                            operation,

                        Kind =
                            CustomerOperationKind.Create,

                        CreateRequest =
                            DeserializePayload<CreateCustomerRequest>(
                                operation)
                    };
                }

                if (IsOperation(
                        operation,
                        UpdateOperation))
                {
                    var request =
                        DeserializePayload<UpdateCustomerRequest>(
                            operation);

                    var serverEntityId =
                        GetServerEntityId(
                            operation,
                            request.Id);

                    if (serverEntityId == Guid.Empty)
                    {
                        errorMessage =
                            "ServerEntityId is required for Customer " +
                            "Update.";

                        return null;
                    }

                    return new ParsedCustomerOperation
                    {
                        Operation =
                            operation,

                        Kind =
                            CustomerOperationKind.Update,

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
                            "ServerEntityId is required for Customer " +
                            "Delete.";

                        return null;
                    }

                    return new ParsedCustomerOperation
                    {
                        Operation =
                            operation,

                        Kind =
                            CustomerOperationKind.Delete,

                        ServerEntityId =
                            serverEntityId
                    };
                }

                errorMessage =
                    $"Unsupported Customer operation " +
                    $"'{operation.Operation}'.";

                return null;
            }
            catch (JsonException exception)
            {
                errorMessage =
                    Truncate(
                        $"Invalid Customer payload: " +
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
            ParsedCustomerOperation parsedOperation)
        {
            return parsedOperation.Kind switch
            {
                CustomerOperationKind.Create =>
                    parsedOperation.CreateRequest?.Name,

                CustomerOperationKind.Update =>
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
            Guid customerId)
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
                customerId);
        }

        private static void RemoveNameOwner(
            IDictionary<string, HashSet<Guid>> nameOwners,
            string name,
            Guid customerId)
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
                customerId);

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

        private static bool IsNameOwnedByAnotherCustomer(
            IDictionary<string, HashSet<Guid>> nameOwners,
            string name,
            Guid customerId)
        {
            return nameOwners.TryGetValue(
                       name,
                       out var owners) &&
                   owners.Any(ownerId =>
                       ownerId != customerId);
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
                            "No Customer synchronization result " +
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

        private enum CustomerOperationKind
        {
            Create,
            Update,
            Delete
        }

        private sealed class ParsedCustomerOperation
        {
            public required SyncBatchOperationRequest Operation
            {
                get;
                init;
            }

            public required CustomerOperationKind Kind
            {
                get;
                init;
            }

            public Guid ServerEntityId
            {
                get;
                init;
            }

            public CreateCustomerRequest? CreateRequest
            {
                get;
                init;
            }

            public UpdateCustomerRequest? UpdateRequest
            {
                get;
                init;
            }
        }

        private sealed class CustomerNameOwner
        {
            public Guid CustomerId { get; set; }

            public string Name { get; set; } =
                string.Empty;
        }
    }
}