using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Results;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inventory.LocalDB.Services;

public sealed class LocalReturnService : ILocalReturnService
{
    private const string ReturnEntityName = "Return";
    private const string RefundCashMovementType = "Refund";

    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public LocalReturnService(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<LocalReturnableSaleResult>>
    SearchSalesAsync(
        string search,
        int maximumResults = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        maximumResults =
            Math.Clamp(
                maximumResults,
                1,
                100);

        var query =
            _db.Sales
                .AsNoTracking()
                .Include(sale =>
                    sale.Lines)
                .Where(sale =>
                    sale.TenantId == tenantId &&
                    sale.Status ==
                        LocalSaleStatus.Completed);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term =
                search.Trim();

            var barcodeTerm =
                new string(
                    term
                        .Where(char.IsDigit)
                        .ToArray());

            var isBarcodeSearch =
                barcodeTerm.Length >= 12;

            query =
                query.Where(sale =>
                    sale.LocalInvoiceNumber.Contains(
                        term) ||

                    (sale.ServerInvoiceNumber != null &&
                     sale.ServerInvoiceNumber.Contains(
                         term)) ||

                    (sale.ReceiptBarcodeValue != null &&
                     (
                         sale.ReceiptBarcodeValue == term ||

                         (isBarcodeSearch &&
                          sale.ReceiptBarcodeValue ==
                              barcodeTerm)
                     )));
        }

        var sales =
            await query
                .OrderByDescending(sale =>
                    sale.SaleDateUtc)
                .Take(maximumResults)
                .ToListAsync(
                    cancellationToken);

        return await BuildReturnableResultsAsync(
            sales,
            tenantId,
            cancellationToken);
    }

    public async Task<LocalReturnableSaleResult?>
        GetReturnableSaleAsync(
            Guid localSaleId,
            CancellationToken cancellationToken = default)
    {
        if (localSaleId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Local sale id is required.",
                nameof(localSaleId));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var sale =
            await _db.Sales
                .AsNoTracking()
                .Include(item =>
                    item.Lines)
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == localSaleId &&
                        item.Status ==
                            LocalSaleStatus.Completed,
                    cancellationToken);

        if (sale ==
            null)
        {
            return null;
        }

        var results =
            await BuildReturnableResultsAsync(
                new List<LocalSale>
                {
                    sale
                },
                tenantId,
                cancellationToken);

        return results
            .FirstOrDefault();
    }

    public async Task<LocalReturn?> GetByIdAsync(
        Guid localReturnId,
        CancellationToken cancellationToken = default)
    {
        if (localReturnId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Local return id is required.",
                nameof(localReturnId));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        return await _db.Returns
            .AsNoTracking()
            .Include(item =>
                item.Lines)
            .FirstOrDefaultAsync(
                item =>
                    item.TenantId == tenantId &&
                    item.Id == localReturnId,
                cancellationToken);
    }

    public async Task<LocalReturn> CreateAsync(
        LocalReturn localReturn,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            localReturn);

        ValidateRequest(
            localReturn);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var sale =
                await _db.Sales
                    .Include(item =>
                        item.Lines)
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id ==
                                localReturn.LocalSaleId &&
                            item.Status ==
                                LocalSaleStatus.Completed,
                        cancellationToken)
                ?? throw new KeyNotFoundException(
                    "The original local sale was not found.");

            var refundMethod =
                ParseRefundMethod(
                    localReturn.RefundMethod);

            if (refundMethod ==
                LocalRefundMethod.Original)
            {
                throw new InvalidOperationException(
                    "Original refund method is not supported.");
            }

            if (refundMethod ==
                    LocalRefundMethod.Credit &&
                !sale.CustomerLocalId.HasValue)
            {
                throw new InvalidOperationException(
                    "A credit refund requires a customer.");
            }

            /*
             * The current server endpoint always requires an active
             * cash session, even for Card/Credit/Exchange.
             * Keep the same invariant locally.
             */
            var openSession =
                await _db.CashSessions
                    .FirstOrDefaultAsync(
                        session =>
                            session.TenantId == tenantId &&
                            session.Status ==
                                LocalCashSessionStatus.Open,
                        cancellationToken)
                ?? throw new InvalidOperationException(
                    "No open local cash session was found.");

            var sourceLines =
                localReturn.Lines
                    .ToList();

            var sourceLineIds =
                sourceLines
                    .Select(line =>
                        line.LocalSaleLineId)
                    .Distinct()
                    .ToList();

            var saleLineMap =
                sale.Lines
                    .Where(line =>
                        sourceLineIds.Contains(
                            line.Id))
                    .ToDictionary(line =>
                        line.Id);

            if (saleLineMap.Count !=
                sourceLineIds.Count)
            {
                throw new InvalidOperationException(
                    "One or more return lines do not belong to " +
                    "the selected sale.");
            }

            var previousReturned =
                await GetReturnedQuantitiesAsync(
                    tenantId,
                    sourceLineIds,
                    cancellationToken);

            PrepareHeader(
                localReturn,
                sale,
                openSession,
                tenantId);

            localReturn.Lines.Clear();

            foreach (var requestedLine in sourceLines)
            {
                var saleLine =
                    saleLineMap[
                        requestedLine.LocalSaleLineId];

                var returnedQuantity =
                    previousReturned.TryGetValue(
                        saleLine.Id,
                        out var alreadyReturned)
                        ? alreadyReturned
                        : 0m;

                var availableQuantity =
                    RoundQuantity(
                        saleLine.Quantity -
                        returnedQuantity);

                var requestedQuantity =
                    RoundQuantity(
                        requestedLine.Quantity);

                if (requestedQuantity <=
                    0m)
                {
                    throw new InvalidOperationException(
                        $"Return quantity must be greater than zero " +
                        $"for '{saleLine.ProductName}'.");
                }

                if (requestedQuantity >
                    availableQuantity)
                {
                    throw new InvalidOperationException(
                        $"Return quantity for '{saleLine.ProductName}' " +
                        $"exceeds the available quantity. Requested: " +
                        $"{requestedQuantity:0.###}; available: " +
                        $"{availableQuantity:0.###}.");
                }

                if (string.IsNullOrWhiteSpace(
                        requestedLine.Reason))
                {
                    throw new InvalidOperationException(
                        $"A return reason is required for " +
                        $"'{saleLine.ProductName}'.");
                }

                var unitsPerPack =
                    saleLine.IsPack
                        ? RoundQuantity(
                            saleLine.UnitsPerPack)
                        : 1m;

                if (saleLine.IsPack &&
                    unitsPerPack <= 0m)
                {
                    throw new InvalidOperationException(
                        $"Pack '{saleLine.ProductName}' has an invalid " +
                        "UnitsPerPack value.");
                }

                var stockProductLocalId =
                    saleLine.UnitProductLocalId !=
                        Guid.Empty
                        ? saleLine.UnitProductLocalId
                        : saleLine.ProductLocalId;

                var stockProductServerId =
                    saleLine.UnitProductServerId !=
                        Guid.Empty
                        ? saleLine.UnitProductServerId
                        : saleLine.ProductServerId ??
                          Guid.Empty;

                var stockQuantity =
                    saleLine.IsPack
                        ? RoundQuantity(
                            requestedQuantity *
                            unitsPerPack)
                        : requestedQuantity;

                var returnLine =
                    new LocalReturnLine
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        LocalReturnId =
                            localReturn.Id,

                        LocalReturn =
                            localReturn,

                        LocalSaleLineId =
                            saleLine.Id,

                        ServerSaleLineId =
                            null,

                        ProductLocalId =
                            saleLine.ProductLocalId,

                        ProductServerId =
                            saleLine.ProductServerId,

                        ProductName =
                            saleLine.ProductName,

                        ProductBarcode =
                            saleLine.ProductBarcode,

                        Quantity =
                            requestedQuantity,

                        UnitProductLocalId =
                            stockProductLocalId,

                        UnitProductServerId =
                            stockProductServerId ==
                                Guid.Empty
                                ? null
                                : stockProductServerId,

                        UnitQuantity =
                            stockQuantity,

                        IsPack =
                            saleLine.IsPack,

                        UnitsPerPack =
                            unitsPerPack,

                        UnitPrice =
                            CalculateEffectiveUnitPrice(
                                saleLine),

                        VatRate =
                            RoundPercentage(
                                saleLine.VatRate),

                        UnitCostPrice =
                            RoundMoney(
                                saleLine.UnitCostPrice),

                        Reason =
                            NormalizeLineReason(
                                requestedLine.Reason),

                        RestockItem =
                            requestedLine.RestockItem
                    };

                localReturn.Lines.Add(
                    returnLine);
            }

            localReturn.TotalAmount =
                RoundMoney(
                    localReturn.Lines.Sum(line =>
                        line.LineAmount));

            if (localReturn.TotalAmount <=
                0m)
            {
                throw new InvalidOperationException(
                    "Return total amount must be greater than zero.");
            }

            _db.Returns.Add(
                localReturn);

            await ApplyRestockAsync(
                localReturn,
                tenantId,
                cancellationToken);

            await ApplyRefundAsync(
                localReturn,
                sale,
                openSession,
                refundMethod,
                tenantId,
                cancellationToken);

            CreateSyncQueueItem(
                localReturn,
                tenantId);

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return localReturn;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    private async Task<IReadOnlyList<LocalReturnableSaleResult>>
        BuildReturnableResultsAsync(
            IReadOnlyCollection<LocalSale> sales,
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        if (sales.Count ==
            0)
        {
            return Array.Empty<LocalReturnableSaleResult>();
        }

        var saleLineIds =
            sales
                .SelectMany(sale =>
                    sale.Lines)
                .Select(line =>
                    line.Id)
                .Distinct()
                .ToList();

        var returnedQuantities =
            await GetReturnedQuantitiesAsync(
                tenantId,
                saleLineIds,
                cancellationToken);

        var customerIds =
            sales
                .Where(sale =>
                    sale.CustomerLocalId.HasValue)
                .Select(sale =>
                    sale.CustomerLocalId!.Value)
                .Distinct()
                .ToList();

        var customerNames =
            await _db.Customers
                .AsNoTracking()
                .Where(customer =>
                    customer.TenantId == tenantId &&
                    customerIds.Contains(
                        customer.Id))
                .ToDictionaryAsync(
                    customer =>
                        customer.Id,
                    customer =>
                        customer.Name,
                    cancellationToken);

        var results =
            new List<LocalReturnableSaleResult>();

        foreach (var sale in sales)
        {
            var lines =
                new List<LocalReturnableSaleLineResult>();

            foreach (var saleLine in sale.Lines)
            {
                var returned =
                    returnedQuantities.TryGetValue(
                        saleLine.Id,
                        out var value)
                        ? value
                        : 0m;

                var available =
                    RoundQuantity(
                        Math.Max(
                            0m,
                            saleLine.Quantity -
                            returned));

                if (available <=
                    0m)
                {
                    continue;
                }

                var unitProductLocalId =
                    saleLine.UnitProductLocalId !=
                        Guid.Empty
                        ? saleLine.UnitProductLocalId
                        : saleLine.ProductLocalId;

                var unitProductServerId =
                    saleLine.UnitProductServerId !=
                        Guid.Empty
                        ? saleLine.UnitProductServerId
                        : saleLine.ProductServerId ??
                          Guid.Empty;

                lines.Add(
                    new LocalReturnableSaleLineResult
                    {
                        LocalSaleLineId =
                            saleLine.Id,

                        ProductLocalId =
                            saleLine.ProductLocalId,

                        ProductServerId =
                            saleLine.ProductServerId,

                        ProductName =
                            saleLine.ProductName,

                        ProductBarcode =
                            saleLine.ProductBarcode,

                        SoldQuantity =
                            saleLine.Quantity,

                        ReturnedQuantity =
                            returned,

                        AvailableQuantity =
                            available,

                        EffectiveUnitPrice =
                            CalculateEffectiveUnitPrice(
                                saleLine),

                        VatRate =
                            saleLine.VatRate,

                        UnitProductLocalId =
                            unitProductLocalId,

                        UnitProductServerId =
                            unitProductServerId ==
                                Guid.Empty
                                ? null
                                : unitProductServerId,

                        IsPack =
                            saleLine.IsPack,

                        UnitsPerPack =
                            saleLine.IsPack
                                ? saleLine.UnitsPerPack
                                : 1m,

                        UnitCostPrice =
                            saleLine.UnitCostPrice
                    });
            }

            customerNames.TryGetValue(
                sale.CustomerLocalId ??
                Guid.Empty,
                out var customerName);

            results.Add(
                new LocalReturnableSaleResult
                {
                    LocalSaleId =
                        sale.Id,

                    ServerSaleId =
                        sale.ServerId,

                    LocalInvoiceNumber =
                        sale.LocalInvoiceNumber,

                    ServerInvoiceNumber =
                        sale.ServerInvoiceNumber,

                    SaleDateUtc =
                        sale.SaleDateUtc,

                    CustomerLocalId =
                        sale.CustomerLocalId,

                    CustomerServerId =
                        sale.CustomerServerId,

                    CustomerName =
                        customerName,

                    TotalAmount =
                        sale.TotalAmount,

                    SyncStatus =
                        sale.SyncStatus,

                    Lines =
                        lines
                });
        }

        return results;
    }

    private async Task<Dictionary<Guid, decimal>>
        GetReturnedQuantitiesAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> saleLineIds,
            CancellationToken cancellationToken)
    {
        if (saleLineIds.Count ==
            0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var processedReturnIds =
            await _db.Returns
                .AsNoTracking()
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.IsProcessed)
                .Select(item =>
                    item.Id)
                .ToListAsync(
                    cancellationToken);

        if (processedReturnIds.Count ==
            0)
        {
            return new Dictionary<Guid, decimal>();
        }

        return await _db.ReturnLines
            .AsNoTracking()
            .Where(line =>
                line.TenantId == tenantId &&
                saleLineIds.Contains(
                    line.LocalSaleLineId) &&
                processedReturnIds.Contains(
                    line.LocalReturnId))
            .GroupBy(line =>
                line.LocalSaleLineId)
            .ToDictionaryAsync(
                group =>
                    group.Key,
                group =>
                    group.Sum(line =>
                        line.Quantity),
                cancellationToken);
    }

    private static void ValidateRequest(
        LocalReturn localReturn)
    {
        if (localReturn.LocalSaleId ==
            Guid.Empty)
        {
            throw new InvalidOperationException(
                "An original local sale is required.");
        }

        if (localReturn.Lines == null ||
            localReturn.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one return line is required.");
        }

        if (string.IsNullOrWhiteSpace(
                localReturn.RefundMethod))
        {
            throw new InvalidOperationException(
                "A refund method is required.");
        }

        if (localReturn.Reason?.Length >
            1000)
        {
            throw new InvalidOperationException(
                "Return notes cannot exceed 1000 characters.");
        }
    }

    private static void PrepareHeader(
        LocalReturn localReturn,
        LocalSale sale,
        LocalCashSession openSession,
        Guid tenantId)
    {
        var now =
            DateTime.UtcNow;

        if (localReturn.Id ==
            Guid.Empty)
        {
            localReturn.Id =
                Guid.NewGuid();
        }

        if (localReturn.ClientOperationId ==
            Guid.Empty)
        {
            localReturn.ClientOperationId =
                Guid.NewGuid();
        }

        if (string.IsNullOrWhiteSpace(
                localReturn.LocalReturnNumber))
        {
            localReturn.LocalReturnNumber =
                GenerateLocalReturnNumber(
                    now);
        }

        localReturn.TenantId =
            tenantId;

        localReturn.ServerId =
            null;

        localReturn.ServerReturnNumber =
            null;

        localReturn.LocalSaleId =
            sale.Id;

        localReturn.ServerSaleId =
            sale.ServerId;

        localReturn.LocalCashSessionId =
            openSession.Id;

        localReturn.CashSessionServerId =
            openSession.ServerId;

        localReturn.CustomerLocalId =
            sale.CustomerLocalId;

        localReturn.CustomerServerId =
            sale.CustomerServerId;

        localReturn.OriginalLocalInvoiceNumber =
            sale.LocalInvoiceNumber;

        localReturn.OriginalServerInvoiceNumber =
            sale.ServerInvoiceNumber;

        localReturn.RefundMethod =
            localReturn.RefundMethod.Trim();

        localReturn.Reason =
            NormalizeHeaderReason(
                localReturn.Reason);

        localReturn.ReturnDateUtc =
            localReturn.ReturnDateUtc == default
                ? now
                : EnsureUtc(
                    localReturn.ReturnDateUtc);

        localReturn.IsProcessed =
            true;

        localReturn.ProcessedAtUtc =
            now;

        localReturn.SyncStatus =
            SyncQueueStatus.Pending;

        localReturn.CreatedAtUtc =
            localReturn.CreatedAtUtc == default
                ? now
                : EnsureUtc(
                    localReturn.CreatedAtUtc);

        localReturn.LastSyncedAtUtc =
            null;
    }

    private async Task ApplyRestockAsync(
        LocalReturn localReturn,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var restockLines =
            localReturn.Lines
                .Where(line =>
                    line.RestockItem)
                .ToList();

        if (restockLines.Count ==
            0)
        {
            return;
        }

        var productIds =
            restockLines
                .Select(line =>
                    line.UnitProductLocalId)
                .Distinct()
                .ToList();

        var products =
            await _db.Products
                .Where(product =>
                    product.TenantId == tenantId &&
                    productIds.Contains(
                        product.Id) &&
                    !product.IsDeletedLocally)
                .ToListAsync(
                    cancellationToken);

        var productMap =
            products.ToDictionary(product =>
                product.Id);

        var stocks =
            await _db.Stocks
                .Where(stock =>
                    stock.TenantId == tenantId &&
                    productIds.Contains(
                        stock.ProductLocalId))
                .ToListAsync(
                    cancellationToken);

        var stockMap =
            stocks.ToDictionary(stock =>
                stock.ProductLocalId);

        foreach (var line in restockLines)
        {
            if (!productMap.TryGetValue(
                    line.UnitProductLocalId,
                    out var stockProduct))
            {
                throw new InvalidOperationException(
                    $"The stock product for '{line.ProductName}' " +
                    "was not found locally.");
            }

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
                            stockProduct.Barcode,

                        Quantity =
                            0m,

                        ReservedQuantity =
                            0m,

                        LastUpdatedUtc =
                            DateTime.UtcNow
                    };

                _db.Stocks.Add(
                    stock);

                stockMap[
                    stockProduct.Id] =
                    stock;
            }

            var quantityBefore =
                RoundQuantity(
                    stock.Quantity);

            var quantityToAdd =
                line.UnitQuantity >
                    0m
                    ? RoundQuantity(
                        line.UnitQuantity)
                    : RoundQuantity(
                        line.Quantity);

            var quantityAfter =
                RoundQuantity(
                    quantityBefore +
                    quantityToAdd);

            stock.Quantity =
                quantityAfter;

            stock.LastUpdatedUtc =
                DateTime.UtcNow;

            stockProduct.LocalStockQuantity =
                quantityAfter;

            _db.StockMovements.Add(
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

                    ProductLocalId =
                        stockProduct.Id,

                    ProductServerId =
                        stockProduct.ServerId ??
                        Guid.Empty,

                    ProductName =
                        stockProduct.Name,

                    ProductBarcode =
                        stockProduct.Barcode,

                    QuantityChange =
                        quantityToAdd,

                    QuantityBefore =
                        quantityBefore,

                    QuantityAfter =
                        quantityAfter,

                    Type =
                        LocalStockMovementType.Return,

                    UnitCost =
                        line.UnitCostPrice,

                    LocalReferenceId =
                        localReturn.Id,

                    ServerReferenceId =
                        localReturn.ServerId,

                    ReferenceNumber =
                        localReturn.LocalReturnNumber,

                    Notes =
                        BuildStockMovementNotes(
                            localReturn,
                            line,
                            quantityToAdd),

                    MovementDateUtc =
                        localReturn.ReturnDateUtc,

                    SyncStatus =
                        SyncQueueStatus.Pending,

                    LastSyncedAtUtc =
                        null
                });
        }
    }

    private async Task ApplyRefundAsync(
        LocalReturn localReturn,
        LocalSale sale,
        LocalCashSession openSession,
        string refundMethod,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        switch (refundMethod)
        {
            case LocalRefundMethod.Cash:
                await ApplyCashRefundAsync(
                    localReturn,
                    openSession,
                    tenantId,
                    cancellationToken);
                break;

            case LocalRefundMethod.Credit:
                await ApplyCreditRefundAsync(
                    localReturn,
                    sale,
                    tenantId,
                    cancellationToken);
                break;

            case LocalRefundMethod.Card:
            case LocalRefundMethod.Exchange:
                break;

            case LocalRefundMethod.Original:
                throw new InvalidOperationException(
                    "Original refund method is not supported.");

            default:
                throw new InvalidOperationException(
                    $"Unsupported refund method: {refundMethod}.");
        }
    }

    private async Task ApplyCashRefundAsync(
        LocalReturn localReturn,
        LocalCashSession openSession,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var movementTotal =
            await _db.CashMovements
                .Where(movement =>
                    movement.TenantId == tenantId &&
                    movement.LocalCashSessionId ==
                        openSession.Id)
                .SumAsync(
                    movement =>
                        (decimal?)movement.Amount,
                    cancellationToken)
            ?? 0m;

        var drawerBalance =
            RoundMoney(
                openSession.OpeningAmount +
                movementTotal);

        var balanceAfter =
            RoundMoney(
                drawerBalance -
                localReturn.TotalAmount);

        if (balanceAfter <
            0m)
        {
            throw new InvalidOperationException(
                $"Cash drawer cannot go negative. Available: " +
                $"€{drawerBalance:F2}; refund: " +
                $"€{localReturn.TotalAmount:F2}.");
        }

        _db.CashMovements.Add(
            new LocalCashMovement
            {
                Id =
                    Guid.NewGuid(),

                TenantId =
                    tenantId,

                ServerId =
                    null,

                ClientOperationId =
                    Guid.NewGuid(),

                LocalCashSessionId =
                    openSession.Id,

                ServerCashSessionId =
                    openSession.ServerId,

                Type =
                    RefundCashMovementType,

                /*
                 * Cash entering the drawer is positive.
                 * Refund cash leaving the drawer is negative.
                 */
                Amount =
                    -localReturn.TotalAmount,

                ReferenceNumber =
                    localReturn.LocalReturnNumber,

                LocalReferenceId =
                    localReturn.Id,

                ServerReferenceId =
                    localReturn.ServerId,

                Notes =
                    $"Cash refund for return " +
                    $"{localReturn.LocalReturnNumber}",

                MovementDateUtc =
                    localReturn.ReturnDateUtc,

                SyncStatus =
                    SyncQueueStatus.Pending
            });
    }

    private async Task ApplyCreditRefundAsync(
        LocalReturn localReturn,
        LocalSale sale,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!sale.CustomerLocalId.HasValue)
        {
            throw new InvalidOperationException(
                "A credit refund requires a local customer.");
        }

        var customer =
            await _db.Customers
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id ==
                            sale.CustomerLocalId.Value &&
                        !item.IsDeleted,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "The local customer was not found.");

        customer.CurrentBalance =
            RoundMoney(
                customer.CurrentBalance -
                localReturn.TotalAmount);
    }

    private void CreateSyncQueueItem(
        LocalReturn localReturn,
        Guid tenantId)
    {
        var payloadJson =
            JsonSerializer.Serialize(
                localReturn,
                new JsonSerializerOptions
                {
                    WriteIndented =
                        false,

                    ReferenceHandler =
                        ReferenceHandler.IgnoreCycles
                });

        _db.SyncQueueItems.Add(
            new SyncQueueItem
            {
                Id =
                    Guid.NewGuid(),

                TenantId =
                    tenantId,

                LocalEntityId =
                    localReturn.Id,

                ClientOperationId =
                    localReturn.ClientOperationId,

                EntityName =
                    ReturnEntityName,

                Operation =
                    SyncOperation.Create,

                PayloadJson =
                    payloadJson,

                Status =
                    SyncQueueStatus.Pending,

                Attempts =
                    0,

                CreatedAtUtc =
                    DateTime.UtcNow,

                ProcessedAtUtc =
                    null,

                ErrorMessage =
                    null
            });
    }

    private static decimal CalculateEffectiveUnitPrice(
        LocalSaleLine saleLine)
    {
        if (saleLine.Quantity <=
            0m)
        {
            throw new InvalidOperationException(
                $"Sale line '{saleLine.ProductName}' has an invalid " +
                "quantity.");
        }

        var gross =
            RoundMoney(
                saleLine.Quantity *
                saleLine.UnitPrice);

        var percentageDiscount =
            RoundMoney(
                gross *
                saleLine.DiscountPercent /
                100m);

        var totalDiscount =
            Math.Clamp(
                percentageDiscount +
                saleLine.DiscountAmount,
                0m,
                gross);

        var net =
            RoundMoney(
                gross -
                totalDiscount);

        return RoundMoney(
            net /
            saleLine.Quantity);
    }

    private static string ParseRefundMethod(
        string value)
    {
        if (string.Equals(
                value,
                LocalRefundMethod.Cash,
                StringComparison.OrdinalIgnoreCase))
        {
            return LocalRefundMethod.Cash;
        }

        if (string.Equals(
                value,
                LocalRefundMethod.Card,
                StringComparison.OrdinalIgnoreCase))
        {
            return LocalRefundMethod.Card;
        }

        if (string.Equals(
                value,
                LocalRefundMethod.Credit,
                StringComparison.OrdinalIgnoreCase))
        {
            return LocalRefundMethod.Credit;
        }

        if (string.Equals(
                value,
                LocalRefundMethod.Exchange,
                StringComparison.OrdinalIgnoreCase))
        {
            return LocalRefundMethod.Exchange;
        }

        if (string.Equals(
                value,
                LocalRefundMethod.Original,
                StringComparison.OrdinalIgnoreCase))
        {
            return LocalRefundMethod.Original;
        }

        throw new InvalidOperationException(
            $"Unsupported refund method: {value}.");
    }

    private static string BuildStockMovementNotes(
        LocalReturn localReturn,
        LocalReturnLine line,
        decimal stockQuantity)
    {
        var value =
            line.IsPack
                ? $"Offline return {localReturn.LocalReturnNumber}: " +
                  $"{line.Quantity:0.###} pack(s) " +
                  $"'{line.ProductName}' x " +
                  $"{line.UnitsPerPack:0.###} = " +
                  $"{stockQuantity:0.###} stock unit(s)."
                : $"Offline return {localReturn.LocalReturnNumber}: " +
                  $"'{line.ProductName}'.";

        return value.Length <=
               500
            ? value
            : value[..500];
    }

    private static string GenerateLocalReturnNumber(
        DateTime utcNow)
    {
        return
            $"RET-LOC-{utcNow:yyyyMMdd-HHmmssfff}-" +
            Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpperInvariant();
    }

    private static string? NormalizeHeaderReason(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        var trimmed =
            value.Trim();

        return trimmed.Length <=
               1000
            ? trimmed
            : trimmed[..1000];
    }

    private static string NormalizeLineReason(
        string value)
    {
        var trimmed =
            value.Trim();

        return trimmed.Length <=
               500
            ? trimmed
            : trimmed[..500];
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

    private static decimal RoundPercentage(
        decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static DateTime EnsureUtc(
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
}
