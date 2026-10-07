using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Products.Requests;
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
    public sealed class ProductSyncBatchOperationHandler
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

        public ProductSyncBatchOperationHandler(
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
            "Product";

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
                new List<ParsedProductOperation>(
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

            var requestedCatalogIds =
                 parsedOperations
                     .Where(parsed =>
                         parsed.Kind ==
                         ProductOperationKind.Create &&
                         parsed.CreateRequest!.CatalogProductId.HasValue &&
                         parsed.CreateRequest.CatalogProductId.Value != Guid.Empty)
                     .Select(parsed =>
                         parsed.CreateRequest!.CatalogProductId!.Value)
                     .Distinct()
                     .ToArray();

            var catalogsById =
                requestedCatalogIds.Length == 0
                    ? new Dictionary<Guid, ProductCatalog>()
                    : await _db.ProductCatalogs
                        .AsNoTracking()
                        .Where(catalog =>
                            requestedCatalogIds.Contains(
                                catalog.Id))
                        .ToDictionaryAsync(
                            catalog =>
                                catalog.Id,
                            cancellationToken);

            var targetProductIds =
                parsedOperations
                    .Where(parsed =>
                        parsed.Kind ==
                            ProductOperationKind.Update ||
                        parsed.Kind ==
                            ProductOperationKind.Delete)
                    .Select(parsed =>
                        parsed.ServerEntityId)
                    .Where(id =>
                        id != Guid.Empty)
                    .Distinct()
                    .ToArray();

            var productsById =
                targetProductIds.Length == 0
                    ? new Dictionary<Guid, Product>()
                    : await _db.Products
                        .Where(product =>
                            product.TenantId == tenantId &&
                            targetProductIds.Contains(
                                product.Id))
                        .ToDictionaryAsync(
                            product =>
                                product.Id,
                            cancellationToken);

            var existingCatalogOwners =
                requestedCatalogIds.Length == 0
                    ? new List<ProductCatalogOwner>()
                    : await _db.Products
                        .AsNoTracking()
                        .Where(product =>
                            product.TenantId == tenantId &&
                            !product.IsDeleted &&
                            product.CatalogProductId.HasValue &&
                            requestedCatalogIds.Contains(
                                product.CatalogProductId.Value))
                        .Select(product =>
                            new ProductCatalogOwner
                            {
                                ProductId =
                                    product.Id,

                                CatalogProductId =
                                    product.CatalogProductId!.Value
                            })
                        .ToListAsync(
                            cancellationToken);

            var catalogOwners =
                existingCatalogOwners
                    .GroupBy(owner =>
                        owner.CatalogProductId)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.First().ProductId);

            foreach (var product in productsById.Values)
            {
                if (!product.IsDeleted &&
                    product.CatalogProductId.HasValue &&
                    product.CatalogProductId.Value != Guid.Empty)
                {
                    catalogOwners[
                        product.CatalogProductId.Value] =
                            product.Id;
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
                    case ProductOperationKind.Create:
                        itemResult =
                            ProcessCreate(
                                parsedOperation,
                                tenantId,
                                userId,
                                catalogsById,
                                catalogOwners,
                                productsById);
                        break;

                    case ProductOperationKind.Update:
                        itemResult =
                            ProcessUpdate(
                                parsedOperation,
                                userId,
                                productsById);
                        break;

                    case ProductOperationKind.Delete:
                        itemResult =
                            ProcessDelete(
                                parsedOperation,
                                userId,
                                catalogOwners,
                                productsById);
                        break;

                    default:
                        itemResult =
                            Conflict(
                                operation,
                                $"Unsupported Product operation " +
                                $"'{operation.Operation}'.");
                        break;
                }

                results[operation.QueueItemId] =
                    itemResult;
            }

            /*
             * SyncBatchOperationExecutor owns the transaction and the
             * single SaveChangesAsync call for this complete batch.
             */
            return OrderResults(
                operations,
                results);
        }

        private SyncBatchItemResult ProcessCreate(
     ParsedProductOperation parsedOperation,
     Guid tenantId,
     Guid userId,
     IReadOnlyDictionary<Guid, ProductCatalog> catalogsById,
     IDictionary<Guid, Guid> catalogOwners,
     IDictionary<Guid, Product> productsById)
        {
            var operation =
                parsedOperation.Operation;

            var request =
                parsedOperation.CreateRequest!;

            var validationError =
                ValidateCreateRequest(
                    request);

            if (validationError != null)
            {
                return Conflict(
                    operation,
                    validationError);
            }

            ProductCatalog? catalog = null;

            var hasCatalog =
                request.CatalogProductId.HasValue &&
                request.CatalogProductId.Value != Guid.Empty;

            if (hasCatalog)
            {
                if (!catalogsById.TryGetValue(
                        request.CatalogProductId!.Value,
                        out catalog) ||
                    catalog.IsDeleted)
                {
                    return Conflict(
                        operation,
                        $"Product Catalog " +
                        $"'{request.CatalogProductId.Value}' was not found.");
                }

                if (catalogOwners.ContainsKey(
                        request.CatalogProductId.Value))
                {
                    return Conflict(
                        operation,
                        $"Product '{catalog.Name}' already exists " +
                        "for this store.");
                }
            }

            /*
             * Custom product barcode uniqueness
             * is scoped to the tenant.
             */
            if (!hasCatalog &&
                !string.IsNullOrWhiteSpace(request.Barcode))
            {
                var normalizedBarcode =
                    request.Barcode.Trim();

                var barcodeExists =
                    _db.Products
                        .Any(product =>
                            product.TenantId == tenantId &&
                            !product.IsDeleted &&
                            product.Barcode == normalizedBarcode);

                if (barcodeExists)
                {
                    return Conflict(
                        operation,
                        $"Barcode '{normalizedBarcode}' already exists " +
                        "for this store.");
                }
            }

            var now =
                DateTime.UtcNow;

            var product =
                _mapper.Map<Product>(
                    request);

            product.Id =
                Guid.NewGuid();

            product.TenantId =
                tenantId;

            product.CatalogProductId =
                hasCatalog
                    ? request.CatalogProductId
                    : null;

            if (catalog != null)
            {
                product.Name =
                    catalog.Name;

                product.Sku =
                    catalog.InternalCode;

                product.Barcode =
                    catalog.Barcode;

                product.Brand =
                    catalog.Brand;

                product.Description =
                    catalog.Description;

                product.Unit =
                    catalog.UnitOfMeasure;
            }
            else
            {
                product.Name =
                    request.Name!.Trim();

                product.Sku =
                    string.IsNullOrWhiteSpace(request.Sku)
                        ? null
                        : request.Sku.Trim();

                product.Barcode =
                    string.IsNullOrWhiteSpace(request.Barcode)
                        ? null
                        : request.Barcode.Trim();

                product.Description =
                    string.IsNullOrWhiteSpace(request.Description)
                        ? null
                        : request.Description.Trim();

                product.Category =
                    string.IsNullOrWhiteSpace(request.Category)
                        ? null
                        : request.Category.Trim();

                product.Brand =
                    string.IsNullOrWhiteSpace(request.Brand)
                        ? null
                        : request.Brand.Trim();

                product.Unit =
                    string.IsNullOrWhiteSpace(request.Unit)
                        ? "pcs"
                        : request.Unit.Trim();
            }

            product.CreatedAt =
                now;

            product.ModifiedAt =
                now;

            product.CreatedByUserId =
                userId;

            product.IsDeleted =
                false;

            _db.Products.Add(
                product);

            productsById[product.Id] =
                product;

            if (product.CatalogProductId.HasValue &&
                product.CatalogProductId.Value != Guid.Empty)
            {
                catalogOwners[
                    product.CatalogProductId.Value] =
                        product.Id;
            }

            return Done(
                operation,
                product.Id);
        }

        private SyncBatchItemResult ProcessUpdate(
            ParsedProductOperation parsedOperation,
            Guid userId,
            IReadOnlyDictionary<Guid, Product> productsById)
        {
            var operation =
                parsedOperation.Operation;

            var request =
                parsedOperation.UpdateRequest!;

            var serverEntityId =
                parsedOperation.ServerEntityId;

            if (!productsById.TryGetValue(
                    serverEntityId,
                    out var product) ||
                product.IsDeleted)
            {
                return Conflict(
                    operation,
                    $"Product '{serverEntityId}' was not found.");
            }

            var validationError =
                ValidateUpdateRequest(
                    request);

            if (validationError != null)
            {
                return Conflict(
                    operation,
                    validationError);
            }

            _mapper.Map(
                request,
                product);

            product.ModifiedAt =
                DateTime.UtcNow;

            product.ModifiedByUserId =
                userId;

            return Done(
                operation,
                product.Id);
        }

        private static SyncBatchItemResult ProcessDelete(
            ParsedProductOperation parsedOperation,
            Guid userId,
            IDictionary<Guid, Guid> catalogOwners,
            IReadOnlyDictionary<Guid, Product> productsById)
        {
            var operation =
                parsedOperation.Operation;

            var serverEntityId =
                parsedOperation.ServerEntityId;

            if (!productsById.TryGetValue(
                    serverEntityId,
                    out var product) ||
                product.IsDeleted)
            {
                return Conflict(
                    operation,
                    $"Product '{serverEntityId}' was not found.");
            }

            product.IsDeleted =
                true;

            product.DeletedAt =
                DateTime.UtcNow;

            product.DeletedByUserId =
                userId;

            if (product.CatalogProductId.HasValue &&
                 product.CatalogProductId.Value != Guid.Empty &&
                 catalogOwners.TryGetValue(
                     product.CatalogProductId.Value, out var ownerId) && ownerId == product.Id)
            {
                catalogOwners.Remove(
                    product.CatalogProductId.Value);
            }

            return Done(
                operation,
                product.Id);
        }

        private static ParsedProductOperation? TryParseOperation(
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
                    var request =
                        DeserializePayload<CreateProductRequest>(
                            operation);

                    return new ParsedProductOperation
                    {
                        Operation =
                            operation,

                        Kind =
                            ProductOperationKind.Create,

                        CreateRequest =
                            request
                    };
                }

                if (IsOperation(
                        operation,
                        UpdateOperation))
                {
                    var serverEntityId =
                        GetRequiredServerEntityId(
                            operation);

                    var request =
                        DeserializePayload<UpdateProductRequest>(
                            operation);

                    return new ParsedProductOperation
                    {
                        Operation =
                            operation,

                        Kind =
                            ProductOperationKind.Update,

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
                    return new ParsedProductOperation
                    {
                        Operation =
                            operation,

                        Kind =
                            ProductOperationKind.Delete,

                        ServerEntityId =
                            GetRequiredServerEntityId(
                                operation)
                    };
                }

                errorMessage =
                    $"Unsupported Product operation " +
                    $"'{operation.Operation}'.";

                return null;
            }
            catch (JsonException exception)
            {
                errorMessage =
                    $"Invalid Product payload: " +
                    exception.Message;

                return null;
            }
            catch (InvalidOperationException exception)
            {
                errorMessage =
                    exception.Message;

                return null;
            }
        }

        private static string? ValidateCreateRequest(
    CreateProductRequest request)
        {
            var hasCatalog =
                request.CatalogProductId.HasValue &&
                request.CatalogProductId.Value != Guid.Empty;

            if (!hasCatalog &&
                string.IsNullOrWhiteSpace(request.Name))
            {
                return "Product name is required when no catalog product is selected.";
            }

            if (!string.IsNullOrWhiteSpace(request.Name) &&
                request.Name.Length > 200)
            {
                return "Product name cannot exceed 200 characters.";
            }

            if (!string.IsNullOrWhiteSpace(request.Sku) &&
                request.Sku.Length > 100)
            {
                return "SKU cannot exceed 100 characters.";
            }

            if (!string.IsNullOrWhiteSpace(request.Barcode) &&
                request.Barcode.Length > 100)
            {
                return "Barcode cannot exceed 100 characters.";
            }

            if (!string.IsNullOrWhiteSpace(request.Description) &&
                request.Description.Length > 1000)
            {
                return "Description cannot exceed 1000 characters.";
            }

            if (!string.IsNullOrWhiteSpace(request.Category) &&
                request.Category.Length > 100)
            {
                return "Category cannot exceed 100 characters.";
            }

            if (!string.IsNullOrWhiteSpace(request.Brand) &&
                request.Brand.Length > 100)
            {
                return "Brand cannot exceed 100 characters.";
            }

            if (!string.IsNullOrWhiteSpace(request.Unit) &&
                request.Unit.Length > 50)
            {
                return "Unit cannot exceed 50 characters.";
            }

            return ValidateValues(
                request.SalePrice,
                request.SalePrice2,
                request.SalePrice3,
                request.PurchasePrice,
                request.VatRate,
                request.MinStockLevel,
                request.MaxStockLevel);
        }

        private static string? ValidateUpdateRequest(
            UpdateProductRequest request)
        {
            return ValidateValues(
                request.SalePrice,
                request.SalePrice2,
                request.SalePrice3,
                request.PurchasePrice,
                request.VatRate,
                request.MinStockLevel,
                request.MaxStockLevel);
        }

        private static string? ValidateValues(
            decimal salePrice,
            decimal salePrice2,
            decimal salePrice3,
            decimal purchasePrice,
            decimal vatRate,
            decimal minStockLevel,
            decimal maxStockLevel)
        {
            if (salePrice < 0 ||
                salePrice2 < 0 ||
                salePrice3 < 0)
            {
                return "Sale prices must be greater than or equal to 0.";
            }

            if (purchasePrice < 0)
            {
                return "Purchase price must be greater than or equal to 0.";
            }

            if (purchasePrice > salePrice ||
                purchasePrice > salePrice2 ||
                purchasePrice > salePrice3)
            {
                return "Purchase price cannot be greater than any sale price.";
            }

            if (vatRate < 0 ||
                vatRate > 100)
            {
                return "VAT rate must be between 0 and 100.";
            }

            if (minStockLevel < 0 ||
                maxStockLevel < 0)
            {
                return "Stock levels must be greater than or equal to 0.";
            }

            if (minStockLevel > maxStockLevel)
            {
                return "Min stock cannot be greater than max stock.";
            }

            return null;
        }

        private static Guid GetRequiredServerEntityId(
            SyncBatchOperationRequest operation)
        {
            if (operation.ServerEntityId.HasValue &&
                operation.ServerEntityId.Value != Guid.Empty)
            {
                return operation.ServerEntityId.Value;
            }

            throw new InvalidOperationException(
                "ServerEntityId is required for Product " +
                $"{operation.Operation}.");
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
                        : Failed(
                            operation,
                            "Product operation produced no result."))
                .ToArray();
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

        private static SyncBatchItemResult Failed(
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
                    SyncBatchItemStatus.Failed,

                ErrorMessage =
                    Truncate(
                        errorMessage)
            };
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

        private sealed class ParsedProductOperation
        {
            public required SyncBatchOperationRequest Operation
            {
                get;
                init;
            }

            public ProductOperationKind Kind
            {
                get;
                init;
            }

            public Guid ServerEntityId
            {
                get;
                init;
            }

            public CreateProductRequest? CreateRequest
            {
                get;
                init;
            }

            public UpdateProductRequest? UpdateRequest
            {
                get;
                init;
            }
        }

        private sealed class ProductCatalogOwner
        {
            public Guid ProductId
            {
                get;
                init;
            }

            public Guid CatalogProductId
            {
                get;
                init;
            }
        }

        private enum ProductOperationKind
        {
            Create,
            Update,
            Delete
        }
    }
}