using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Requests;
using Inventory.LocalDB.Services.Results;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Inventory.LocalDB.Services;

public sealed class LocalPurchaseService
    : ILocalPurchaseService
{
    private const string PurchaseEntityName = "Purchase";
    private const string PurchaseMovementType = "Purchase";

    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public LocalPurchaseService(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<LocalPurchaseResult> CreateCompleteAsync(
        CreateLocalPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateCreateRequest(request);

        foreach (var requestLine in request.Lines)
        {
            ValidateLine(requestLine);
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var now =
                DateTime.UtcNow;

            var supplier =
                await _db.Suppliers
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id == request.SupplierLocalId &&
                            !item.IsDeleted,
                        cancellationToken);

            if (supplier == null)
            {
                throw new InvalidOperationException(
                    "The selected supplier was not found locally.");
            }

            if (!supplier.IsActive)
            {
                throw new InvalidOperationException(
                    $"Supplier '{supplier.Name}' is inactive.");
            }

            var duplicateProduct =
                request.Lines
                    .GroupBy(line =>
                        line.ProductLocalId)
                    .FirstOrDefault(group =>
                        group.Count() > 1);

            if (duplicateProduct != null)
            {
                throw new InvalidOperationException(
                    $"Product '{duplicateProduct.Key}' appears more " +
                    "than once in the purchase.");
            }

            var productIds =
                request.Lines
                    .Select(line =>
                        line.ProductLocalId)
                    .Distinct()
                    .ToList();

            var purchasedProducts =
                await _db.Products
                    .Where(product =>
                        product.TenantId == tenantId &&
                        productIds.Contains(product.Id) &&
                        !product.IsDeletedLocally)
                    .ToListAsync(cancellationToken);

            if (purchasedProducts.Count != productIds.Count)
            {
                var foundIds =
                    purchasedProducts
                        .Select(product =>
                            product.Id)
                        .ToHashSet();

                var missingIds =
                    productIds
                        .Where(id =>
                            !foundIds.Contains(id))
                        .ToList();

                throw new InvalidOperationException(
                    "One or more local products were not found: " +
                    string.Join(", ", missingIds));
            }

            var inactivePurchasedProduct =
                purchasedProducts
                    .FirstOrDefault(product =>
                        !product.IsActive);

            if (inactivePurchasedProduct != null)
            {
                throw new InvalidOperationException(
                    $"Product '{inactivePurchasedProduct.Name}' is inactive.");
            }

            var purchasedProductMap =
                purchasedProducts
                    .ToDictionary(product =>
                        product.Id);

            /*
             * A purchase line always keeps the selected product.
             *
             * For a normal product:
             *     purchased product == stock product.
             *
             * For a pack:
             *     purchased product == pack;
             *     stock product == linked unit product;
             *     stock quantity == pack quantity * units per pack.
             */
            var stockResolutions =
                await ResolveStockProductsAsync(
                    tenantId,
                    request.Lines,
                    purchasedProductMap,
                    cancellationToken);

            var stockProductIds =
                stockResolutions
                    .Select(resolution =>
                        resolution.StockProduct.Id)
                    .Distinct()
                    .ToList();

            var stocks =
                await _db.Stocks
                    .Where(stock =>
                        stock.TenantId == tenantId &&
                        stockProductIds.Contains(
                            stock.ProductLocalId))
                    .ToListAsync(cancellationToken);

            var stockMap =
                stocks.ToDictionary(
                    stock =>
                        stock.ProductLocalId);

            var purchaseDateUtc =
                request.PurchaseDateUtc == default
                    ? now
                    : NormalizeUtc(
                        request.PurchaseDateUtc);

            var purchase =
                new LocalPurchase
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    ServerId =
                        null,

                    ClientOperationId =
                        Guid.NewGuid(),

                    SupplierLocalId =
                        supplier.Id,

                    SupplierServerId =
                        supplier.ServerId,

                    LocalPurchaseNumber =
                        GeneratePurchaseNumber(),

                    ServerPurchaseNumber =
                        null,

                    SupplierInvoiceNumber =
                        NormalizeNullable(
                            request.SupplierInvoiceNumber),

                    Status =
                        LocalPurchaseStatus.Received,

                    PurchaseDateUtc =
                        purchaseDateUtc,

                    ExpectedDeliveryDateUtc =
                        NormalizeNullableUtc(
                            request.ExpectedDeliveryDateUtc),

                    DeliveryDateUtc =
                        purchaseDateUtc,

                    PaymentDueDateUtc =
                        supplier.PaymentTermsDays > 0
                            ? purchaseDateUtc.AddDays(
                                supplier.PaymentTermsDays)
                            : purchaseDateUtc,

                    PaymentDateUtc =
                        null,

                    Notes =
                        NormalizeNullable(
                            request.Notes),

                    SyncStatus =
                        SyncQueueStatus.Pending,

                    CreatedAtUtc =
                        now
                };

            decimal totalAmountExclVat = 0m;
            decimal totalVatAmount = 0m;
            decimal totalAmountInclVat = 0m;

            foreach (var resolution in stockResolutions)
            {
                var requestLine =
                    resolution.RequestLine;

                var purchasedProduct =
                    resolution.PurchasedProduct;

                var stockProduct =
                    resolution.StockProduct;

                var purchaseQuantity =
                    resolution.PurchaseQuantity;

                var stockQuantity =
                    resolution.StockQuantity;

                var unitPurchasePrice =
                    RoundMoney(
                        requestLine.UnitPurchasePrice);

                var vatRate =
                    Math.Round(
                        requestLine.VatRate,
                        2,
                        MidpointRounding.AwayFromZero);

                /*
                 * Financial values are always calculated from the
                 * selected purchase product.
                 *
                 * Example:
                 *     2 packs x EUR 15 = EUR 30 excl. VAT.
                 *
                 * The stock conversion does not change the financial
                 * quantity or the purchase-line product.
                 */
                var lineAmountExclVat =
                    RoundMoney(
                        purchaseQuantity *
                        unitPurchasePrice);

                var vatAmount =
                    RoundMoney(
                        lineAmountExclVat *
                        vatRate /
                        100m);

                var lineAmountInclVat =
                    lineAmountExclVat +
                    vatAmount;

                var purchaseLine =
                    new LocalPurchaseLine
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        LocalPurchaseId =
                            purchase.Id,

                        LocalPurchase =
                            purchase,

                        /*
                         * Keep the selected pack/product here.
                         * The uploader sends this ProductServerId to the
                         * API, where the server applies its own pack logic.
                         */
                        ProductLocalId =
                            purchasedProduct.Id,

                        ProductServerId =
                            purchasedProduct.ServerId,

                        ProductName =
                            purchasedProduct.Name,

                        ProductBarcode =
                            NormalizeNullable(
                                purchasedProduct.Barcode),

                        QuantityOrdered =
                            purchaseQuantity,

                        QuantityReceived =
                            purchaseQuantity,

                        UnitPurchasePrice =
                            unitPurchasePrice,

                        VatRate =
                            vatRate
                    };

                purchase.Lines.Add(
                    purchaseLine);

                totalAmountExclVat +=
                    lineAmountExclVat;

                totalVatAmount +=
                    vatAmount;

                totalAmountInclVat +=
                    lineAmountInclVat;

                /*
                 * For a pack, stockProduct is the linked unit product.
                 * For a normal product, it is the purchased product.
                 */
                if (!stockMap.TryGetValue(
                        stockProduct.Id,
                        out var stock))
                {
                    stock =
                        new LocalStock
                        {
                            Id =
                                Guid.NewGuid(),

                            TenantId =
                                tenantId,

                            ServerId =
                                null,

                            ProductLocalId =
                                stockProduct.Id,

                            ProductServerId =
                                stockProduct.ServerId,

                            ProductName =
                                stockProduct.Name,

                            ProductBarcode =
                                NormalizeNullable(
                                    stockProduct.Barcode),

                            Quantity =
                                0m,

                            ReservedQuantity =
                                0m,

                            LastUpdatedUtc =
                                now
                        };

                    _db.Stocks.Add(
                        stock);

                    stockMap.Add(
                        stockProduct.Id,
                        stock);
                }

                var quantityBefore =
                    stock.Quantity;

                var quantityAfter =
                    RoundQuantity(
                        quantityBefore +
                        stockQuantity);

                stock.ProductServerId =
                    stockProduct.ServerId;

                stock.ProductName =
                    stockProduct.Name;

                stock.ProductBarcode =
                    NormalizeNullable(
                        stockProduct.Barcode);

                stock.Quantity =
                    quantityAfter;

                stock.LastUpdatedUtc =
                    now;

                /*
                 * LocalStockQuantity belongs to the product whose stock
                 * is actually maintained. For packs this is the unit
                 * product, not the pack product.
                 */
                stockProduct.LocalStockQuantity =
                    quantityAfter;

                var movementNotes =
                    resolution.IsPack
                        ? BuildPackMovementNotes(
                            purchase.LocalPurchaseNumber,
                            purchasedProduct.Name,
                            stockProduct.Name,
                            purchaseQuantity,
                            resolution.UnitsPerPack,
                            stockQuantity)
                        : $"Local purchase receipt " +
                          $"{purchase.LocalPurchaseNumber} for " +
                          $"'{purchasedProduct.Name}'.";

                var stockMovement =
                    new LocalStockMovement
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        ServerId =
                            null,

                        ClientOperationId =
                            Guid.NewGuid(),

                        /*
                         * A stock movement targets the actual stored
                         * product. Therefore packs target their unit
                         * product here.
                         */
                        ProductLocalId =
                            stockProduct.Id,

                        /*
                         * Guid.Empty is allowed while the product is still
                         * local-only. Product synchronization will later
                         * populate the dependent server identifier.
                         */
                        ProductServerId =
                            stockProduct.ServerId ??
                            Guid.Empty,

                        ProductName =
                            stockProduct.Name,

                        ProductBarcode =
                            NormalizeNullable(
                                stockProduct.Barcode),

                        Type =
                            PurchaseMovementType,

                        QuantityChange =
                            stockQuantity,

                        QuantityBefore =
                            quantityBefore,

                        QuantityAfter =
                            quantityAfter,

                        /*
                         * For packs, convert pack cost to cost per unit.
                         */
                        UnitCost =
                            resolution.StockUnitCost,

                        LocalReferenceId =
                            purchase.Id,

                        ServerReferenceId =
                            null,

                        ReferenceNumber =
                            purchase.LocalPurchaseNumber,

                        Notes =
                            movementNotes,

                        SyncStatus =
                            SyncQueueStatus.Pending
                    };

                _db.StockMovements.Add(
                    stockMovement);
            }

            purchase.TotalAmountExclVat =
                RoundMoney(
                    totalAmountExclVat);

            purchase.TotalVatAmount =
                RoundMoney(
                    totalVatAmount);

            purchase.TotalAmountInclVat =
                RoundMoney(
                    totalAmountInclVat);

            _db.Purchases.Add(
                purchase);

            _db.SyncQueueItems.Add(
                new SyncQueueItem
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    ClientOperationId =
                        purchase.ClientOperationId,

                    LocalEntityId =
                        purchase.Id,

                    EntityName =
                        PurchaseEntityName,

                    Operation =
                        SyncOperation.Create,

                    Status =
                        SyncQueueStatus.Pending,

                    Attempts =
                        0,

                    ErrorMessage =
                        null,

                    PayloadJson =
                        null,

                    CreatedAtUtc =
                        now
                });

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return ToResult(
                purchase);
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    public async Task<LocalPurchaseResult> GetByIdAsync(
        Guid localPurchaseId,
        CancellationToken cancellationToken = default)
    {
        if (localPurchaseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Purchase id is required.",
                nameof(localPurchaseId));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var purchase =
            await _db.Purchases
                .AsNoTracking()
                .Include(item =>
                    item.Lines)
                .Include(item =>
                    item.Payments)
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == localPurchaseId,
                    cancellationToken);

        if (purchase == null)
        {
            throw new KeyNotFoundException(
                "Local purchase was not found.");
        }

        return ToResult(
            purchase);
    }

    public async Task<IReadOnlyList<LocalPurchaseResult>>
        GetRecentAsync(
            int take = 50,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        take =
            Math.Clamp(
                take,
                1,
                200);

        var purchases =
            await _db.Purchases
                .AsNoTracking()
                .Include(item =>
                    item.Lines)
                .Where(item =>
                    item.TenantId == tenantId)
                .OrderByDescending(item =>
                    item.PurchaseDateUtc)
                .ThenByDescending(item =>
                    item.CreatedAtUtc)
                .Take(take)
                .ToListAsync(cancellationToken);

        return purchases
            .Select(ToResult)
            .ToList();
    }

    public async Task<IReadOnlyList<LocalPurchaseResult>>
        GetPendingAsync(
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var purchases =
            await _db.Purchases
                .AsNoTracking()
                .Include(item =>
                    item.Lines)
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.SyncStatus !=
                        SyncQueueStatus.Done)
                .OrderBy(item =>
                    item.CreatedAtUtc)
                .ToListAsync(cancellationToken);

        return purchases
            .Select(ToResult)
            .ToList();
    }

    private async Task<List<LocalPurchaseStockResolution>>
        ResolveStockProductsAsync(
            Guid tenantId,
            IReadOnlyCollection<CreateLocalPurchaseLineRequest> requestLines,
            IReadOnlyDictionary<Guid, LocalProduct> purchasedProducts,
            CancellationToken cancellationToken)
    {
        var packProducts =
            purchasedProducts.Values
                .Where(product =>
                    product.IsPack)
                .ToList();

        var unitLocalIds =
            packProducts
                .Where(product =>
                    product.UnitProductLocalId.HasValue &&
                    product.UnitProductLocalId.Value !=
                        Guid.Empty)
                .Select(product =>
                    product.UnitProductLocalId!.Value)
                .Distinct()
                .ToList();

        var unitServerIds =
            packProducts
                .Where(product =>
                    (!product.UnitProductLocalId.HasValue ||
                     product.UnitProductLocalId.Value ==
                        Guid.Empty) &&
                    product.UnitProductServerId.HasValue &&
                    product.UnitProductServerId.Value !=
                        Guid.Empty)
                .Select(product =>
                    product.UnitProductServerId!.Value)
                .Distinct()
                .ToList();

        var unitProducts =
            await _db.Products
                .Where(product =>
                    product.TenantId == tenantId &&
                    !product.IsDeletedLocally &&
                    (
                        unitLocalIds.Contains(
                            product.Id) ||
                        (
                            product.ServerId.HasValue &&
                            unitServerIds.Contains(
                                product.ServerId.Value)
                        )
                    ))
                .ToListAsync(cancellationToken);

        var unitProductsByLocalId =
            unitProducts
                .GroupBy(product =>
                    product.Id)
                .ToDictionary(
                    group => group.Key,
                    group => group.First());

        var unitProductsByServerId =
            unitProducts
                .Where(product =>
                    product.ServerId.HasValue &&
                    product.ServerId.Value !=
                        Guid.Empty)
                .GroupBy(product =>
                    product.ServerId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.First());

        var resolutions =
            new List<LocalPurchaseStockResolution>();

        foreach (var requestLine in requestLines)
        {
            if (!purchasedProducts.TryGetValue(
                    requestLine.ProductLocalId,
                    out var purchasedProduct))
            {
                throw new InvalidOperationException(
                    $"Local product '{requestLine.ProductLocalId}' " +
                    "was not found.");
            }

            var purchaseQuantity =
                RoundQuantity(
                    requestLine.Quantity);

            var purchaseUnitPrice =
                RoundMoney(
                    requestLine.UnitPurchasePrice);

            if (!purchasedProduct.IsPack)
            {
                resolutions.Add(
                    new LocalPurchaseStockResolution
                    {
                        RequestLine =
                            requestLine,

                        PurchasedProduct =
                            purchasedProduct,

                        StockProduct =
                            purchasedProduct,

                        PurchaseQuantity =
                            purchaseQuantity,

                        StockQuantity =
                            purchaseQuantity,

                        StockUnitCost =
                            purchaseUnitPrice,

                        IsPack =
                            false,

                        UnitsPerPack =
                            1m
                    });

                continue;
            }

            if (purchasedProduct.UnitsPerPack <= 0m)
            {
                throw new InvalidOperationException(
                    $"Pack '{purchasedProduct.Name}' has an invalid " +
                    "UnitsPerPack value.");
            }

            LocalProduct? unitProduct =
                null;

            if (purchasedProduct.UnitProductLocalId.HasValue &&
                purchasedProduct.UnitProductLocalId.Value !=
                    Guid.Empty)
            {
                unitProductsByLocalId.TryGetValue(
                    purchasedProduct.UnitProductLocalId.Value,
                    out unitProduct);
            }

            if (unitProduct == null &&
                purchasedProduct.UnitProductServerId.HasValue &&
                purchasedProduct.UnitProductServerId.Value !=
                    Guid.Empty)
            {
                unitProductsByServerId.TryGetValue(
                    purchasedProduct.UnitProductServerId.Value,
                    out unitProduct);
            }

            if (unitProduct == null)
            {
                throw new InvalidOperationException(
                    $"The unit product linked to pack " +
                    $"'{purchasedProduct.Name}' was not found locally. " +
                    "Synchronize products before creating this purchase.");
            }

            if (unitProduct.Id ==
                purchasedProduct.Id)
            {
                throw new InvalidOperationException(
                    $"Pack '{purchasedProduct.Name}' cannot reference itself.");
            }

            if (unitProduct.IsPack)
            {
                throw new InvalidOperationException(
                    $"Pack '{purchasedProduct.Name}' points to another " +
                    $"pack '{unitProduct.Name}'. Nested packs are not supported.");
            }

            if (!unitProduct.IsActive ||
                unitProduct.IsDeletedLocally)
            {
                throw new InvalidOperationException(
                    $"The unit product '{unitProduct.Name}' linked to pack " +
                    $"'{purchasedProduct.Name}' is inactive.");
            }

            var unitsPerPack =
                RoundQuantity(
                    purchasedProduct.UnitsPerPack);

            var stockQuantity =
                RoundQuantity(
                    purchaseQuantity *
                    unitsPerPack);

            if (stockQuantity <= 0m)
            {
                throw new InvalidOperationException(
                    $"Pack '{purchasedProduct.Name}' produced an invalid " +
                    "stock quantity.");
            }

            var stockUnitCost =
                RoundMoney(
                    purchaseUnitPrice /
                    unitsPerPack);

            resolutions.Add(
                new LocalPurchaseStockResolution
                {
                    RequestLine =
                        requestLine,

                    PurchasedProduct =
                        purchasedProduct,

                    StockProduct =
                        unitProduct,

                    PurchaseQuantity =
                        purchaseQuantity,

                    StockQuantity =
                        stockQuantity,

                    StockUnitCost =
                        stockUnitCost,

                    IsPack =
                        true,

                    UnitsPerPack =
                        unitsPerPack
                });
        }

        return resolutions;
    }

    private static void ValidateCreateRequest(
        CreateLocalPurchaseRequest request)
    {
        if (request.SupplierLocalId ==
            Guid.Empty)
        {
            throw new InvalidOperationException(
                "A supplier is required.");
        }

        if (request.Lines == null ||
            request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "The purchase must contain at least one line.");
        }

        if (request.SupplierInvoiceNumber?.Length >
            100)
        {
            throw new InvalidOperationException(
                "Supplier invoice number cannot exceed 100 characters.");
        }

        if (request.Notes?.Length >
            1000)
        {
            throw new InvalidOperationException(
                "Notes cannot exceed 1000 characters.");
        }
    }

    private static void ValidateLine(
        CreateLocalPurchaseLineRequest line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (line.ProductLocalId ==
            Guid.Empty)
        {
            throw new InvalidOperationException(
                "Every purchase line must contain a product.");
        }

        if (line.Quantity <=
            0m)
        {
            throw new InvalidOperationException(
                "Purchase quantity must be greater than zero.");
        }

        if (line.UnitPurchasePrice <
            0m)
        {
            throw new InvalidOperationException(
                "Unit purchase price cannot be negative.");
        }

        if (line.VatRate < 0m ||
            line.VatRate > 100m)
        {
            throw new InvalidOperationException(
                "VAT rate must be between 0 and 100.");
        }
    }

    private static LocalPurchaseResult ToResult(
        LocalPurchase purchase)
    {
        return new LocalPurchaseResult
        {
            Id =
                purchase.Id,

            ServerId =
                purchase.ServerId,

            ClientOperationId =
                purchase.ClientOperationId,

            SupplierLocalId =
                purchase.SupplierLocalId,

            SupplierServerId =
                purchase.SupplierServerId,

            LocalPurchaseNumber =
                purchase.LocalPurchaseNumber,

            ServerPurchaseNumber =
                purchase.ServerPurchaseNumber,

            SupplierInvoiceNumber =
                purchase.SupplierInvoiceNumber,

            TotalAmountExclVat =
                purchase.TotalAmountExclVat,

            TotalVatAmount =
                purchase.TotalVatAmount,

            TotalAmountInclVat =
                purchase.TotalAmountInclVat,

            Status =
                purchase.Status,

            SyncStatus =
                purchase.SyncStatus,

            PurchaseDateUtc =
                purchase.PurchaseDateUtc,

            ExpectedDeliveryDateUtc =
                purchase.ExpectedDeliveryDateUtc,

            DeliveryDateUtc =
                purchase.DeliveryDateUtc,

            Notes =
                purchase.Notes,

            CreatedAtUtc =
                purchase.CreatedAtUtc,

            LastSyncedAtUtc =
                purchase.LastSyncedAtUtc,

            Lines =
                purchase.Lines
                    .Select(ToLineResult)
                    .ToList()
        };
    }

    private static LocalPurchaseLineResult ToLineResult(
        LocalPurchaseLine line)
    {
        return new LocalPurchaseLineResult
        {
            Id =
                line.Id,

            ProductLocalId =
                line.ProductLocalId,

            ProductServerId =
                line.ProductServerId,

            ProductName =
                line.ProductName,

            ProductBarcode =
                line.ProductBarcode,

            QuantityOrdered =
                line.QuantityOrdered,

            QuantityReceived =
                line.QuantityReceived,

            UnitPurchasePrice =
                line.UnitPurchasePrice,

            VatRate =
                line.VatRate,

            LineAmountExclVat =
                line.LineAmountExclVat,

            VatAmount =
                line.VatAmount,

            LineAmountInclVat =
                line.LineAmountInclVat
        };
    }

    private static string BuildPackMovementNotes(
        string purchaseNumber,
        string packName,
        string unitProductName,
        decimal packQuantity,
        decimal unitsPerPack,
        decimal unitQuantity)
    {
        var notes =
            $"Local purchase {purchaseNumber}: " +
            $"{FormatQuantity(packQuantity)} pack(s) " +
            $"'{packName}' x {FormatQuantity(unitsPerPack)} " +
            $"unit(s) = {FormatQuantity(unitQuantity)} unit(s) " +
            $"of '{unitProductName}'.";

        return notes.Length <= 500
            ? notes
            : notes[..500];
    }

    private static string GeneratePurchaseNumber()
    {
        return
            $"PUR-{DateTime.UtcNow:yyyyMMddHHmmssfff}-" +
            Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpperInvariant();
    }

    private static decimal RoundMoney(
        decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static decimal RoundQuantity(
        decimal value)
    {
        return Math.Round(
            value,
            3,
            MidpointRounding.AwayFromZero);
    }

    private static string FormatQuantity(
        decimal value)
    {
        return value.ToString(
            "0.###",
            CultureInfo.InvariantCulture);
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc =>
                value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            _ =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc)
        };
    }

    private static DateTime? NormalizeNullableUtc(
        DateTime? value)
    {
        return value.HasValue
            ? NormalizeUtc(
                value.Value)
            : null;
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed class LocalPurchaseStockResolution
    {
        public required CreateLocalPurchaseLineRequest RequestLine
        {
            get;
            init;
        }

        public required LocalProduct PurchasedProduct
        {
            get;
            init;
        }

        public required LocalProduct StockProduct
        {
            get;
            init;
        }

        public decimal PurchaseQuantity
        {
            get;
            init;
        }

        public decimal StockQuantity
        {
            get;
            init;
        }

        public decimal StockUnitCost
        {
            get;
            init;
        }

        public bool IsPack
        {
            get;
            init;
        }

        public decimal UnitsPerPack
        {
            get;
            init;
        }
    }
}