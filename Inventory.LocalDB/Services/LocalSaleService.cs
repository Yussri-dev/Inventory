using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inventory.LocalDB.Services;

public sealed class LocalSaleService : ILocalSaleService
{
    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public LocalSaleService(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<LocalSale?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Sale id is required.",
                nameof(id));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        return await _db.Sales
            .AsNoTracking()
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .Include(sale => sale.LocalCashSession)
            .FirstOrDefaultAsync(
                sale =>
                    sale.TenantId == tenantId &&
                    sale.Id == id,
                cancellationToken);
    }

    public async Task<List<LocalSale>> GetTodaySalesAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var todayUtc =
            DateTime.UtcNow.Date;

        var tomorrowUtc =
            todayUtc.AddDays(1);

        return await _db.Sales
            .AsNoTracking()
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .Where(sale =>
                sale.TenantId == tenantId &&
                sale.SaleDateUtc >= todayUtc &&
                sale.SaleDateUtc < tomorrowUtc)
            .OrderByDescending(sale =>
                sale.SaleDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<LocalSale> CreateAsync(
    LocalSale sale,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            sale);

        await LocalDatabaseWriteGate.Semaphore.WaitAsync(
            cancellationToken);

        try
        {
            return await CreateCoreAsync(
                sale,
                cancellationToken);
        }
        finally
        {
            LocalDatabaseWriteGate.Semaphore.Release();
        }
    }

    public async Task<LocalSale> CreateCoreAsync(
        LocalSale sale,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);

        ValidateSaleStructure(sale);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var openSession =
                await _db.CashSessions
                    .FirstOrDefaultAsync(
                        session =>
                            session.TenantId == tenantId &&
                            session.Status ==
                                LocalCashSessionStatus.Open,
                        cancellationToken);

            if (openSession == null)
            {
                throw new InvalidOperationException(
                    "No open local cash session was found " +
                    "for the current tenant.");
            }

            PrepareSaleHeader(
                sale,
                tenantId,
                openSession.Id,
                openSession.ServerId);

            RecalculateSaleTotals(sale);

            ValidatePaymentAmount(sale);

            var stockResolutions =
                PrepareStockResolutions(
                    sale);

            await ValidateStockAvailabilityAsync(
                tenantId,
                stockResolutions,
                cancellationToken);

            _db.Sales.Add(sale);

            await ApplyStockMovementsAsync(
                sale,
                tenantId,
                stockResolutions,
                cancellationToken);

            CreateCashMovementIfNeeded(
                sale,
                tenantId,
                openSession.Id,
                openSession.ServerId);

            await ApplyCustomerCreditAsync(
                sale,
                tenantId,
                cancellationToken);

            CreateSyncQueueItem(
                sale,
                tenantId);

            if (string.IsNullOrWhiteSpace(sale.ReceiptBarcodeValue))
            {
                sale.ReceiptBarcodeValue =
                    await GenerateUniqueReceiptBarcodeAsync(
                        sale.TenantId,
                        sale.SaleDateUtc,
                        cancellationToken);
            }

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return sale;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            _db.ChangeTracker.Clear();


            throw;
        }
    }

    private async Task<string> GenerateUniqueReceiptBarcodeAsync(
    Guid tenantId,
    DateTime saleDateUtc,
    CancellationToken cancellationToken)
    {
        const int maximumAttempts =
            10;

        for (var attempt = 1;
             attempt <= maximumAttempts;
             attempt++)
        {
            var candidate =
                GenerateReceiptBarcodeValue(
                    saleDateUtc);

            var alreadyExists =
                await _db.Sales
                    .AsNoTracking()
                    .AnyAsync(
                        sale =>
                            sale.TenantId == tenantId &&
                            sale.ReceiptBarcodeValue == candidate,
                        cancellationToken);

            if (!alreadyExists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Unable to generate a unique receipt barcode.");
    }

    private static string GenerateReceiptBarcodeValue(
    DateTime saleDateUtc)
    {
        var utcDate =
            saleDateUtc.Kind switch
            {
                DateTimeKind.Utc =>
                    saleDateUtc,

                DateTimeKind.Local =>
                    saleDateUtc.ToUniversalTime(),

                _ =>
                    DateTime.SpecifyKind(
                        saleDateUtc,
                        DateTimeKind.Utc)
            };

        /*
         * 17 chiffres :
         * yyyyMMddHHmmssfff
         */
        var datePart =
            utcDate.ToString(
                "yyyyMMddHHmmssfff");

        /*
         * 6 chiffres aléatoires.
         */
        var randomPart =
            RandomNumberGenerator
                .GetInt32(
                    0,
                    1_000_000)
                .ToString("D6");

        return datePart +
               randomPart;
    }

    private static void ValidateSaleStructure(
        LocalSale sale)
    {
        if (sale.Lines == null ||
            sale.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Sale must contain at least one line.");
        }

        if (sale.Payments == null ||
            sale.Payments.Count == 0)
        {
            throw new InvalidOperationException(
                "Sale must contain at least one payment.");
        }

        foreach (var line in sale.Lines)
        {
            if (line.ProductLocalId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Every sale line must contain a local product id.");
            }

            if (line.Quantity <= 0m)
            {
                throw new InvalidOperationException(
                    $"Invalid quantity for product '{line.ProductName}'.");
            }

            if (line.UnitPrice < 0m)
            {
                throw new InvalidOperationException(
                    $"Invalid unit price for product '{line.ProductName}'.");
            }

            if (line.VatRate < 0m ||
                line.VatRate > 100m)
            {
                throw new InvalidOperationException(
                    $"Invalid VAT rate for product '{line.ProductName}'.");
            }

            if (line.DiscountPercent < 0m ||
                line.DiscountPercent > 100m)
            {
                throw new InvalidOperationException(
                    $"Invalid discount percentage for " +
                    $"product '{line.ProductName}'.");
            }

            if (line.DiscountAmount < 0m)
            {
                throw new InvalidOperationException(
                    $"Invalid discount amount for " +
                    $"product '{line.ProductName}'.");
            }

            if (line.IsPack &&
                line.UnitsPerPack <= 0m)
            {
                throw new InvalidOperationException(
                    $"Pack '{line.ProductName}' has an invalid " +
                    "UnitsPerPack value.");
            }

            if (line.IsPack &&
                line.UnitProductLocalId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Pack '{line.ProductName}' has no linked " +
                    "local unit product.");
            }
        }

        foreach (var payment in sale.Payments)
        {
            if (payment.Amount <= 0m)
            {
                throw new InvalidOperationException(
                    "Every payment amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(
                    payment.Method))
            {
                throw new InvalidOperationException(
                    "Every payment requires a payment method.");
            }
        }
    }

    private static void PrepareSaleHeader(
        LocalSale sale,
        Guid tenantId,
        Guid localCashSessionId,
        Guid? serverCashSessionId)
    {
        var now =
            DateTime.UtcNow;

        if (sale.Id == Guid.Empty)
        {
            sale.Id =
                Guid.NewGuid();
        }

        if (sale.ClientOperationId == Guid.Empty)
        {
            sale.ClientOperationId =
                Guid.NewGuid();
        }

        if (string.IsNullOrWhiteSpace(
                sale.LocalInvoiceNumber))
        {
            sale.LocalInvoiceNumber =
                GenerateLocalInvoiceNumber(now);
        }

        sale.TenantId =
            tenantId;

        sale.LocalCashSessionId =
            localCashSessionId;

        sale.CashSessionServerId =
            serverCashSessionId;

        sale.Status =
            LocalSaleStatus.Completed;

        sale.SyncStatus =
            SyncQueueStatus.Pending;

        sale.SaleDateUtc =
            sale.SaleDateUtc == default
                ? now
                : EnsureUtc(
                    sale.SaleDateUtc);

        sale.CreatedAtUtc =
            sale.CreatedAtUtc == default
                ? now
                : EnsureUtc(
                    sale.CreatedAtUtc);

        sale.LastSyncedAtUtc =
            null;

        foreach (var line in sale.Lines)
        {
            if (line.Id == Guid.Empty)
            {
                line.Id =
                    Guid.NewGuid();
            }

            line.TenantId =
                tenantId;

            line.LocalSaleId =
                sale.Id;

            line.LocalSale =
                sale;

            line.Quantity =
                RoundQuantity(
                    line.Quantity);

            line.UnitQuantity =
                RoundQuantity(
                    line.UnitQuantity);

            line.UnitsPerPack =
                RoundQuantity(
                    line.UnitsPerPack <= 0m
                        ? 1m
                        : line.UnitsPerPack);

            line.UnitPrice =
                RoundMoney(
                    line.UnitPrice);

            line.VatRate =
                RoundPercentage(
                    line.VatRate);

            line.DiscountPercent =
                RoundPercentage(
                    line.DiscountPercent);

            line.DiscountAmount =
                RoundMoney(
                    line.DiscountAmount);

            line.UnitCostPrice =
                RoundMoney(
                    line.UnitCostPrice);
        }
        foreach (var payment in sale.Payments)
        {
            if (payment.Id == Guid.Empty)
            {
                payment.Id =
                    Guid.NewGuid();
            }

            payment.TenantId =
                tenantId;

            payment.LocalSaleId =
                sale.Id;

            payment.LocalSale =
                sale;

            payment.Amount =
                RoundMoney(
                    payment.Amount);

            payment.Method =
                payment.Method.Trim();

            payment.TransactionRef =
                NormalizeNullable(
                    payment.TransactionRef);

            payment.PaidAtUtc =
                payment.PaidAtUtc == default
                    ? now
                    : EnsureUtc(
                        payment.PaidAtUtc);

            payment.SyncStatus =
                SyncQueueStatus.Pending;

            payment.LastSyncedAtUtc =
                null;
        }
    }

    private static void RecalculateSaleTotals(
        LocalSale sale)
    {
        decimal subtotalExclVat = 0m;
        decimal totalVat = 0m;
        decimal totalInclVat = 0m;
        decimal totalDiscount = 0m;

        foreach (var line in sale.Lines)
        {
            var grossInclVat =
                RoundMoney(
                    line.Quantity *
                    line.UnitPrice);

            var percentageDiscount =
                RoundMoney(
                    grossInclVat *
                    line.DiscountPercent /
                    100m);

            var lineDiscount =
                Math.Clamp(
                    percentageDiscount +
                    line.DiscountAmount,
                    0m,
                    grossInclVat);

            var netInclVat =
                RoundMoney(
                    grossInclVat -
                    lineDiscount);

            var netExclVat =
                line.VatRate > 0m
                    ? RoundMoney(
                        netInclVat /
                        (1m + line.VatRate / 100m))
                    : netInclVat;

            var vatAmount =
                RoundMoney(
                    netInclVat -
                    netExclVat);

            subtotalExclVat +=
                netExclVat;

            totalVat +=
                vatAmount;

            totalInclVat +=
                netInclVat;

            totalDiscount +=
                lineDiscount;
        }

        sale.SubtotalAmount =
            RoundMoney(
                subtotalExclVat);

        sale.VatAmount =
            RoundMoney(
                totalVat);

        sale.TotalAmount =
            RoundMoney(
                totalInclVat);

        sale.DiscountAmount =
            RoundMoney(
                totalDiscount);

        sale.PaidAmount =
    RoundMoney(
        sale.Payments.Sum(payment =>
            payment.Amount));

        sale.ChangeAmount =
            sale.PaidAmount >
            sale.TotalAmount
                ? RoundMoney(
                    sale.PaidAmount -
                    sale.TotalAmount)
                : 0m;

        var creditAmount =
            RoundMoney(
                sale.Payments
                    .Where(payment =>
                        string.Equals(
                            payment.Method,
                            "Credit",
                            StringComparison.OrdinalIgnoreCase))
                    .Sum(payment =>
                        payment.Amount));

        var cashAndCardAmount =
            RoundMoney(
                sale.Payments
                    .Where(payment =>
                        !string.Equals(
                            payment.Method,
                            "Credit",
                            StringComparison.OrdinalIgnoreCase))
                    .Sum(payment =>
                        payment.Amount));

        var realCashAndCardPaid =
            RoundMoney(
                Math.Max(
                    0m,
                    cashAndCardAmount -
                    sale.ChangeAmount));

        sale.PaymentStatus =
            creditAmount > 0m &&
            realCashAndCardPaid > 0m
                ? LocalPaymentStatus.Partial
                : creditAmount > 0m
                    ? LocalPaymentStatus.Unpaid
                    : realCashAndCardPaid >=
                      sale.TotalAmount
                        ? LocalPaymentStatus.Paid
                        : realCashAndCardPaid > 0m
                            ? LocalPaymentStatus.Partial
                            : LocalPaymentStatus.Unpaid;
    }

    private static void ValidatePaymentAmount(
        LocalSale sale)
    {
        if (sale.TotalAmount <= 0m)
        {
            throw new InvalidOperationException(
                "Sale total amount must be greater than zero.");
        }

        if (sale.PaidAmount <= 0m)
        {
            throw new InvalidOperationException(
                "Paid amount must be greater than zero.");
        }

        if (sale.PaidAmount <
            sale.TotalAmount)
        {
            throw new InvalidOperationException(
                "Paid amount is lower than the sale total. " +
                "Partial payment is not allowed here yet.");
        }

        if (sale.ChangeAmount > 0m)
        {
            var cashPaid =
                sale.Payments
                    .Where(payment =>
                        string.Equals(
                            payment.Method,
                            "Cash",
                            StringComparison.OrdinalIgnoreCase))
                    .Sum(payment =>
                        payment.Amount);

            if (cashPaid <
                sale.ChangeAmount)
            {
                throw new InvalidOperationException(
                    "Change cannot exceed the received cash amount.");
            }
        }
    }

    private static List<LocalSaleStockResolution>
        PrepareStockResolutions(
            LocalSale sale)
    {
        var resolutions =
            new List<LocalSaleStockResolution>();

        foreach (var line in sale.Lines)
        {
            var stockProductLocalId =
                line.IsPack
                    ? line.UnitProductLocalId
                    : line.UnitProductLocalId !=
                      Guid.Empty
                        ? line.UnitProductLocalId
                        : line.ProductLocalId;

            if (stockProductLocalId ==
                Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Stock product is missing for " +
                    $"'{line.ProductName}'.");
            }

            var stockProductServerId =
                line.IsPack
                    ? line.UnitProductServerId
                    : line.UnitProductServerId !=
                      Guid.Empty
                        ? line.UnitProductServerId
                        : line.ProductServerId ??
                          Guid.Empty;

            var stockQuantity =
                CalculateStockQuantityToRemove(
                    line);

            if (stockQuantity <= 0m)
            {
                throw new InvalidOperationException(
                    $"Invalid stock quantity for " +
                    $"'{line.ProductName}'.");
            }

            line.UnitProductLocalId =
                stockProductLocalId;

            line.UnitProductServerId =
                stockProductServerId;

            line.UnitQuantity =
                stockQuantity;

            resolutions.Add(
                new LocalSaleStockResolution
                {
                    Line =
                        line,

                    StockProductLocalId =
                        stockProductLocalId,

                    StockProductServerId =
                        stockProductServerId,

                    StockQuantity =
                        stockQuantity
                });
        }

        return resolutions;
    }

    private async Task ValidateStockAvailabilityAsync(
        Guid tenantId,
        IReadOnlyCollection<LocalSaleStockResolution> resolutions,
        CancellationToken cancellationToken)
    {
        var stockProductIds =
            resolutions
                .Select(resolution =>
                    resolution.StockProductLocalId)
                .Distinct()
                .ToList();

        var stocks =
            await _db.Stocks
                .AsNoTracking()
                .Where(stock =>
                    stock.TenantId == tenantId &&
                    stockProductIds.Contains(
                        stock.ProductLocalId))
                .ToListAsync(cancellationToken);

        var stockMap =
            stocks.ToDictionary(
                stock =>
                    stock.ProductLocalId);

        var requiredByProduct =
            resolutions
                .GroupBy(resolution =>
                    resolution.StockProductLocalId)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        RoundQuantity(
                            group.Sum(resolution =>
                                resolution.StockQuantity)));

        foreach (var requirement in requiredByProduct)
        {
            if (!stockMap.TryGetValue(
                    requirement.Key,
                    out var stock))
            {
                throw new InvalidOperationException(
                    $"No local stock was found for product " +
                    $"'{requirement.Key}'.");
            }

            var availableQuantity =
                stock.AvailableQuantity;

            if (availableQuantity <
                requirement.Value)
            {
                throw new InvalidOperationException(
                    $"Insufficient local stock for product " +
                    $"'{stock.ProductName}'. Required: " +
                    $"{requirement.Value:0.###}, available: " +
                    $"{availableQuantity:0.###}.");
            }
        }
    }

    private async Task ApplyStockMovementsAsync(
        LocalSale sale,
        Guid tenantId,
        IReadOnlyCollection<LocalSaleStockResolution> resolutions,
        CancellationToken cancellationToken)
    {
        var stockProductIds =
            resolutions
                .Select(resolution =>
                    resolution.StockProductLocalId)
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

        var localProducts =
            await _db.Products
                .Where(product =>
                    product.TenantId == tenantId &&
                    stockProductIds.Contains(
                        product.Id))
                .ToListAsync(cancellationToken);

        var productMap =
            localProducts.ToDictionary(
                product =>
                    product.Id);

        foreach (var resolution in resolutions)
        {
            var line =
                resolution.Line;

            var stock =
                stockMap[
                    resolution.StockProductLocalId];

            var quantityBefore =
                stock.Quantity;

            var quantityAfter =
                RoundQuantity(
                    quantityBefore -
                    resolution.StockQuantity);

            if (quantityAfter < 0m)
            {
                throw new InvalidOperationException(
                    $"Stock cannot become negative for " +
                    $"'{stock.ProductName}'.");
            }

            stock.Quantity =
                quantityAfter;

            stock.LastUpdatedUtc =
                DateTime.UtcNow;

            if (resolution.StockProductServerId !=
                Guid.Empty)
            {
                stock.ProductServerId =
                    resolution.StockProductServerId;
            }

            if (productMap.TryGetValue(
                    resolution.StockProductLocalId,
                    out var localProduct))
            {
                localProduct.LocalStockQuantity =
                    quantityAfter;
            }

            var movement =
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
                        resolution.StockProductLocalId,

                    ProductServerId =
                        resolution.StockProductServerId,

                    ProductName =
                        stock.ProductName,

                    ProductBarcode =
                        stock.ProductBarcode,

                    QuantityChange =
                        -resolution.StockQuantity,

                    QuantityBefore =
                        quantityBefore,

                    QuantityAfter =
                        quantityAfter,

                    Type =
                        LocalStockMovementType.Sale,

                    UnitCost =
                        line.UnitCostPrice,

                    LocalReferenceId =
                        sale.Id,

                    ServerReferenceId =
                        sale.ServerId,

                    ReferenceNumber =
                        sale.LocalInvoiceNumber,

                    Notes =
                        BuildStockMovementNotes(
                            sale,
                            line,
                            resolution.StockQuantity),

                    MovementDateUtc =
                        sale.SaleDateUtc,

                    SyncStatus =
                        SyncQueueStatus.Pending
                };

            _db.StockMovements.Add(
                movement);
        }
    }

    private static decimal CalculateStockQuantityToRemove(
        LocalSaleLine line)
    {
        if (line.UnitQuantity > 0m)
        {
            return RoundQuantity(
                line.UnitQuantity);
        }

        if (line.IsPack)
        {
            if (line.UnitsPerPack <= 0m)
            {
                throw new InvalidOperationException(
                    $"Pack '{line.ProductName}' has an invalid " +
                    "UnitsPerPack value.");
            }

            return RoundQuantity(
                line.Quantity *
                line.UnitsPerPack);
        }

        return RoundQuantity(
            line.Quantity);
    }

    private void CreateCashMovementIfNeeded(
        LocalSale sale,
        Guid tenantId,
        Guid localCashSessionId,
        Guid? serverCashSessionId)
    {
        var cashPaid =
            sale.Payments
                .Where(payment =>
                    string.Equals(
                        payment.Method,
                        "Cash",
                        StringComparison.OrdinalIgnoreCase))
                .Sum(payment =>
                    payment.Amount);

        if (cashPaid <= 0m)
        {
            return;
        }

        var cashAmountToDrawer =
            RoundMoney(
                cashPaid -
                sale.ChangeAmount);

        if (cashAmountToDrawer <= 0m)
        {
            return;
        }

        var movement =
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
                    localCashSessionId,

                ServerCashSessionId =
                    serverCashSessionId,

                Type =
                    LocalCashMovementType.Sale,

                Amount =
                    cashAmountToDrawer,

                ReferenceNumber =
                    sale.LocalInvoiceNumber,

                LocalReferenceId =
                    sale.Id,

                ServerReferenceId =
                    sale.ServerId,

                Notes =
                    $"Cash payment for sale " +
                    $"{sale.LocalInvoiceNumber}",

                MovementDateUtc =
                    sale.SaleDateUtc,

                SyncStatus =
                    SyncQueueStatus.Pending
            };

        _db.CashMovements.Add(
            movement);
    }

    private void CreateSyncQueueItem(
        LocalSale sale,
        Guid tenantId)
    {
        var payloadJson =
            JsonSerializer.Serialize(
                sale,
                new JsonSerializerOptions
                {
                    WriteIndented =
                        false,

                    ReferenceHandler =
                        ReferenceHandler.IgnoreCycles
                });

        var queueItem =
            new SyncQueueItem
            {
                Id =
                    Guid.NewGuid(),

                TenantId =
                    tenantId,

                LocalEntityId =
                    sale.Id,

                ClientOperationId =
                    sale.ClientOperationId,

                EntityName =
                    SyncEntityName.Sale,

                Operation =
                    SyncOperation.Create,

                PayloadJson =
                    payloadJson,

                Status =
                    SyncQueueStatus.Pending,

                Attempts =
                    0,

                ErrorMessage =
                    null,

                CreatedAtUtc =
                    DateTime.UtcNow
            };

        _db.SyncQueueItems.Add(
            queueItem);
    }

    private static string BuildStockMovementNotes(
        LocalSale sale,
        LocalSaleLine line,
        decimal stockQuantity)
    {
        var notes =
            line.IsPack
                ? $"Offline sale {sale.LocalInvoiceNumber}: " +
                  $"{line.Quantity:0.###} pack(s) " +
                  $"'{line.ProductName}' x " +
                  $"{line.UnitsPerPack:0.###} unit(s) = " +
                  $"{stockQuantity:0.###} stock unit(s)."
                : $"Offline sale {sale.LocalInvoiceNumber}: " +
                  $"'{line.ProductName}'.";

        return notes.Length <= 500
            ? notes
            : notes[..500];
    }

    private static string GenerateLocalInvoiceNumber(DateTime utcNow)
    {
        return
            $"LOC-{utcNow:yyyyMMdd-HHmmssfff}-" +
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

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    // ============================================================
    // PENDING SALES
    // ============================================================

    private const string PendingSaleStatus = "Pending";

    public async Task<IReadOnlyList<LocalSale>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        return await _db.Sales
            .AsNoTracking()
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .Where(sale =>
                sale.TenantId == tenantId &&
                sale.Status == PendingSaleStatus)
            .OrderByDescending(sale =>
                sale.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<LocalSale> SavePendingAsync(
     LocalSale sale,
     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            sale);

        await LocalDatabaseWriteGate.Semaphore.WaitAsync(
            cancellationToken);

        try
        {
            return await SavePendingCoreAsync(
                sale,
                cancellationToken);
        }
        finally
        {
            LocalDatabaseWriteGate.Semaphore.Release();
        }
    }

    private async Task<LocalSale> SavePendingCoreAsync(
    LocalSale sale,
    CancellationToken cancellationToken)
    {
        ValidatePendingSale(
            sale);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        /*
         * Copier les lignes avant de nettoyer le ChangeTracker.
         * Le graphe envoyé par l'interface ne sera jamais directement
         * attaché comme ancien graphe EF Core.
         */
        var sourceLines =
            sale.Lines
                .Select(CloneSaleLine)
                .ToList();

        var requestedSaleId =
            sale.Id;

        var isNew =
            requestedSaleId == Guid.Empty;

        _db.ChangeTracker.Clear();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var openSession =
                await GetOpenCashSessionAsync(
                    tenantId,
                    cancellationToken);

            LocalSale targetSale;

            if (isNew)
            {
                targetSale =
                    new LocalSale
                    {
                        Id =
                            Guid.NewGuid(),

                        ClientOperationId =
                            sale.ClientOperationId == Guid.Empty
                                ? Guid.NewGuid()
                                : sale.ClientOperationId,

                        LocalInvoiceNumber =
                            sale.LocalInvoiceNumber,

                        CustomerLocalId =
                            sale.CustomerLocalId,

                        CustomerServerId =
                            sale.CustomerServerId,

                        SaleDateUtc =
                            sale.SaleDateUtc,

                        Notes =
                            sale.Notes,

                        CreatedAtUtc =
                            sale.CreatedAtUtc
                    };

                _db.Sales.Add(
                    targetSale);
            }
            else
            {
                targetSale =
                    await _db.Sales
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId &&
                                item.Id == requestedSaleId &&
                                item.Status == PendingSaleStatus,
                            cancellationToken)
                    ?? throw new KeyNotFoundException(
                        "The local pending sale was not found.");

                /*
                 * Suppression directe dans SQLite.
                 * Les anciennes lignes ne sont pas chargées dans le
                 * ChangeTracker.
                 */
                await _db.SaleLines
                    .Where(line =>
                        line.TenantId == tenantId &&
                        line.LocalSaleId == targetSale.Id)
                    .ExecuteDeleteAsync(
                        cancellationToken);

                await _db.Payments
                    .Where(payment =>
                        payment.TenantId == tenantId &&
                        payment.LocalSaleId == targetSale.Id)
                    .ExecuteDeleteAsync(
                        cancellationToken);

                CopyPendingHeaderValues(
                    sale,
                    targetSale);
            }

            foreach (var line in sourceLines)
            {
                targetSale.Lines.Add(
                    line);

                _db.Entry(line).State =
                    EntityState.Added;
            }

            PreparePendingHeader(
                targetSale,
                tenantId,
                openSession.Id,
                openSession.ServerId);

            RecalculateSaleTotals(
                targetSale);

            targetSale.Status =
                PendingSaleStatus;

            targetSale.PaymentStatus =
                LocalPaymentStatus.Unpaid;

            targetSale.SyncStatus =
                SyncQueueStatus.Draft;

            targetSale.PaidAmount =
                0m;

            targetSale.ChangeAmount =
                0m;

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var savedId =
                targetSale.Id;

            _db.ChangeTracker.Clear();

            return await _db.Sales
                .AsNoTracking()
                .Include(item =>
                    item.Lines)
                .Include(item =>
                    item.Payments)
                .FirstAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == savedId,
                    cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var details =
                BuildConcurrencyDetails(
                    exception);

            await transaction.RollbackAsync(
                CancellationToken.None);

            _db.ChangeTracker.Clear();

            throw new InvalidOperationException(
                $"Pending sale concurrency conflict: {details}",
                exception);
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            _db.ChangeTracker.Clear();

            throw;
        }
    }

    public async Task<LocalSale> CompletePendingAsync(
    Guid pendingSaleId,
    LocalSale completedSale,
    CancellationToken cancellationToken = default)
    {
        if (pendingSaleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Pending sale id is required.",
                nameof(pendingSaleId));
        }

        ArgumentNullException.ThrowIfNull(
            completedSale);

        await LocalDatabaseWriteGate.Semaphore.WaitAsync(
            cancellationToken);

        try
        {
            return await CompletePendingCoreAsync(
                pendingSaleId,
                completedSale,
                cancellationToken);
        }
        finally
        {
            LocalDatabaseWriteGate.Semaphore.Release();
        }
    }

    private async Task<LocalSale> CompletePendingCoreAsync(
     Guid pendingSaleId,
     LocalSale completedSale,
     CancellationToken cancellationToken)
    {
        ValidateSaleStructure(
            completedSale);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        /*
         * Créer un nouveau graphe indépendant avant de toucher au
         * ChangeTracker.
         */
        var sourceLines =
            completedSale.Lines
                .Select(CloneSaleLine)
                .ToList();

        var sourcePayments =
            completedSale.Payments
                .Select(ClonePayment)
                .ToList();

        _db.ChangeTracker.Clear();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var pendingSale =
                await _db.Sales
                    .FirstOrDefaultAsync(
                        sale =>
                            sale.TenantId == tenantId &&
                            sale.Id == pendingSaleId,
                        cancellationToken)
                ?? throw new KeyNotFoundException(
                    "The local pending sale was not found.");

            /*
             * Idempotence :
             * une tentative précédente peut avoir réussi malgré la perte
             * du résultat côté interface.
             */
            if (string.Equals(
                    pendingSale.Status,
                    LocalSaleStatus.Completed,
                    StringComparison.OrdinalIgnoreCase))
            {
                await transaction.CommitAsync(
                    cancellationToken);

                _db.ChangeTracker.Clear();

                return await _db.Sales
                    .AsNoTracking()
                    .Include(sale =>
                        sale.Lines)
                    .Include(sale =>
                        sale.Payments)
                    .FirstAsync(
                        sale =>
                            sale.TenantId == tenantId &&
                            sale.Id == pendingSaleId,
                        cancellationToken);
            }

            if (!string.Equals(
                    pendingSale.Status,
                    PendingSaleStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Sale {pendingSale.LocalInvoiceNumber} cannot be " +
                    $"completed. Current status: {pendingSale.Status}.");
            }

            var openSession =
                await GetOpenCashSessionAsync(
                    tenantId,
                    cancellationToken);

            /*
             * Suppression SQL directe des anciennes lignes.
             */
            await _db.SaleLines
                .Where(line =>
                    line.TenantId == tenantId &&
                    line.LocalSaleId == pendingSaleId)
                .ExecuteDeleteAsync(
                    cancellationToken);

            await _db.Payments
                .Where(payment =>
                    payment.TenantId == tenantId &&
                    payment.LocalSaleId == pendingSaleId)
                .ExecuteDeleteAsync(
                    cancellationToken);

            pendingSale.CustomerLocalId =
                completedSale.CustomerLocalId;

            pendingSale.CustomerServerId =
                completedSale.CustomerServerId;

            pendingSale.SaleDateUtc =
                completedSale.SaleDateUtc;

            pendingSale.Notes =
                completedSale.Notes;

            foreach (var line in sourceLines)
            {
                pendingSale.Lines.Add(
                    line);

                /*
                 * Cette ligne doit produire un INSERT, jamais un UPDATE.
                 */
                _db.Entry(line).State =
                    EntityState.Added;
            }

            foreach (var payment in sourcePayments)
            {
                pendingSale.Payments.Add(
                    payment);

                /*
                 * Ce paiement est nouveau. On force explicitement INSERT.
                 */
                _db.Entry(payment).State =
                    EntityState.Added;
            }

            PrepareSaleHeader(
                pendingSale,
                tenantId,
                openSession.Id,
                openSession.ServerId);

            RecalculateSaleTotals(
                pendingSale);

            ValidatePaymentAmount(
                pendingSale);

            var stockResolutions =
                PrepareStockResolutions(
                    pendingSale);

            await ValidateStockAvailabilityAsync(
                tenantId,
                stockResolutions,
                cancellationToken);

            await ApplyStockMovementsAsync(
                pendingSale,
                tenantId,
                stockResolutions,
                cancellationToken);

            await ApplyCustomerCreditAsync(
                pendingSale,
                tenantId,
                cancellationToken);

            CreateCashMovementIfNeeded(
                pendingSale,
                tenantId,
                openSession.Id,
                openSession.ServerId);

            CreateSyncQueueItem(
                pendingSale,
                tenantId);

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            _db.ChangeTracker.Clear();

            return await _db.Sales
                .AsNoTracking()
                .Include(sale =>
                    sale.Lines)
                .Include(sale =>
                    sale.Payments)
                .FirstAsync(
                    sale =>
                        sale.TenantId == tenantId &&
                        sale.Id == pendingSaleId,
                    cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var details =
                BuildConcurrencyDetails(
                    exception);

            await transaction.RollbackAsync(
                CancellationToken.None);

            _db.ChangeTracker.Clear();

            throw new InvalidOperationException(
                $"CompletePending concurrency conflict: {details}",
                exception);
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            _db.ChangeTracker.Clear();

            throw;
        }
    }

    public async Task DeletePendingAsync(
    Guid pendingSaleId,
    CancellationToken cancellationToken = default)
    {
        if (pendingSaleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Pending sale id is required.",
                nameof(pendingSaleId));
        }

        await LocalDatabaseWriteGate.Semaphore.WaitAsync(
            cancellationToken);

        try
        {
            await DeletePendingCoreAsync(
                pendingSaleId,
                cancellationToken);
        }
        finally
        {
            LocalDatabaseWriteGate.Semaphore.Release();
        }
    }

    private async Task<LocalCashSession> GetOpenCashSessionAsync(
    Guid tenantId,
    CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        /*
         * Une session de caisse n'est pas modifiée dans les opérations
         * de vente. Elle est donc chargée sans tracking.
         *
         * Take(2) permet aussi de détecter une corruption locale avec
         * plusieurs sessions ouvertes pour le même tenant.
         */
        var openSessions =
            await _db.CashSessions
                .AsNoTracking()
                .Where(session =>
                    session.TenantId == tenantId &&
                    session.Status ==
                        LocalCashSessionStatus.Open)
                .OrderByDescending(session =>
                    session.OpenedAtUtc)
                .Take(2)
                .ToListAsync(
                    cancellationToken);

        if (openSessions.Count == 0)
        {
            throw new InvalidOperationException(
                "No open local cash session was found " +
                "for the current tenant.");
        }

        if (openSessions.Count > 1)
        {
            throw new InvalidOperationException(
                "Multiple open local cash sessions were found " +
                "for the current tenant. Close or reconcile the " +
                "duplicate sessions before continuing.");
        }

        return openSessions[0];
    }

    private static string BuildConcurrencyDetails(
    DbUpdateConcurrencyException exception)
    {
        var details =
            exception.Entries
                .Select(entry =>
                {
                    var primaryKey =
                        entry.Metadata.FindPrimaryKey();

                    var key =
                        primaryKey == null
                            ? "No primary key"
                            : string.Join(
                                ", ",
                                primaryKey.Properties.Select(property =>
                                    $"{property.Name}=" +
                                    entry.Property(property.Name)
                                        .CurrentValue));

                    return
                        $"Entity={entry.Metadata.ClrType.Name}, " +
                        $"State={entry.State}, " +
                        $"Key=[{key}]";
                });

        return string.Join(
            " | ",
            details);
    }

    private async Task DeletePendingCoreAsync(
     Guid pendingSaleId,
     CancellationToken cancellationToken)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        _db.ChangeTracker.Clear();

        /*
         * LocalSale -> Lines et LocalSale -> Payments utilisent Cascade.
         * SQLite supprimera donc les enfants automatiquement.
         */
        var deletedRows =
            await _db.Sales
                .Where(sale =>
                    sale.TenantId == tenantId &&
                    sale.Id == pendingSaleId &&
                    sale.Status == PendingSaleStatus)
                .ExecuteDeleteAsync(
                    cancellationToken);

        _db.ChangeTracker.Clear();

        if (deletedRows == 0)
        {
            throw new KeyNotFoundException(
                "The local pending sale was not found.");
        }
    }

    private static void ValidatePendingSale(
        LocalSale sale)
    {
        if (sale.Lines == null ||
            sale.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "A pending sale must contain at least one line.");
        }

        foreach (var line in sale.Lines)
        {
            if (line.ProductLocalId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Every pending-sale line requires a local product.");
            }

            if (line.Quantity <= 0m)
            {
                throw new InvalidOperationException(
                    $"Invalid quantity for product '{line.ProductName}'.");
            }

            if (line.UnitPrice < 0m)
            {
                throw new InvalidOperationException(
                    $"Invalid unit price for product '{line.ProductName}'.");
            }

            if (line.VatRate < 0m ||
                line.VatRate > 100m)
            {
                throw new InvalidOperationException(
                    $"Invalid VAT rate for product '{line.ProductName}'.");
            }

            if (line.DiscountPercent < 0m ||
                line.DiscountPercent > 100m)
            {
                throw new InvalidOperationException(
                    $"Invalid discount percentage for " +
                    $"product '{line.ProductName}'.");
            }

            if (line.DiscountAmount < 0m)
            {
                throw new InvalidOperationException(
                    $"Invalid discount amount for " +
                    $"product '{line.ProductName}'.");
            }

            if (line.IsPack &&
                line.UnitsPerPack <= 0m)
            {
                throw new InvalidOperationException(
                    $"Pack '{line.ProductName}' has an invalid " +
                    "UnitsPerPack value.");
            }

            if (line.IsPack &&
                line.UnitProductLocalId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Pack '{line.ProductName}' has no linked " +
                    "unit product.");
            }
        }
    }

    private async Task ApplyCustomerCreditAsync(
    LocalSale sale,
    Guid tenantId,
    CancellationToken cancellationToken)
    {
        var creditAmount =
            RoundMoney(
                sale.Payments
                    .Where(payment =>
                        string.Equals(
                            payment.Method,
                            "Credit",
                            StringComparison.OrdinalIgnoreCase))
                    .Sum(payment =>
                        payment.Amount));

        if (creditAmount <= 0m)
        {
            return;
        }

        if (!sale.CustomerLocalId.HasValue ||
            sale.CustomerLocalId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A customer is required for a credit sale.");
        }

        /*
         * Protection contre une double application du crédit
         * pour la même vente.
         */
        var trackedTransactionExists =
            _db.CustomerTransactions.Local
                .Any(transaction =>
                    transaction.TenantId == tenantId &&
                    transaction.SaleLocalId == sale.Id &&
                    transaction.Origin ==
                        LocalCustomerTransactionOrigin.Sale);

        var transactionAlreadyExists =
            trackedTransactionExists ||
            await _db.CustomerTransactions
                .AsNoTracking()
                .AnyAsync(
                    transaction =>
                        transaction.TenantId == tenantId &&
                        transaction.SaleLocalId == sale.Id &&
                        transaction.Origin ==
                            LocalCustomerTransactionOrigin.Sale,
                    cancellationToken);

        if (transactionAlreadyExists)
        {
            return;
        }

        var customer =
            await _db.Customers
                .FirstOrDefaultAsync(
                    customer =>
                        customer.TenantId == tenantId &&
                        customer.Id ==
                            sale.CustomerLocalId.Value &&
                        !customer.IsDeleted,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                $"The local customer " +
                $"'{sale.CustomerLocalId.Value}' was not found.");

        if (!customer.AllowCredit)
        {
            throw new InvalidOperationException(
                "Credit is not enabled for this customer.");
        }

        var balanceBefore =
            RoundMoney(
                customer.CurrentBalance);

        var balanceAfter =
            RoundMoney(
                balanceBefore +
                creditAmount);

        /*
         * HasUnlimitedCredit désactive la limite.
         * Sinon CreditLimit contient la dette maximale autorisée.
         */
        if (!customer.HasUnlimitedCredit &&
            balanceAfter > customer.CreditLimit)
        {
            throw new InvalidOperationException(
                $"The customer credit limit would be exceeded. " +
                $"Limit: {customer.CreditLimit:0.00}, " +
                $"new balance: {balanceAfter:0.00}.");
        }

        var now =
     DateTime.UtcNow;

        customer.CurrentBalance =
            balanceAfter;

        customer.ModifiedAtUtc =
            now;

        var customerTransaction =
            new LocalCustomerTransaction
            {
                Id =
                    Guid.NewGuid(),

                TenantId =
                    tenantId,

                ServerId =
                    null,

                ClientOperationId =
                    Guid.NewGuid(),

                CustomerLocalId =
                    customer.Id,

                CustomerServerId =
                    customer.ServerId,

                SaleLocalId =
                    sale.Id,

                SaleServerId =
                    sale.ServerId,

                Type =
                    LocalCustomerTransactionType.Credit,

                Origin =
                    LocalCustomerTransactionOrigin.Sale,

                UploadRequired =
                    false,

                IsCash =
                    false,

                Amount =
                    creditAmount,

                BalanceBefore =
                    balanceBefore,

                BalanceAfter =
                    balanceAfter,

                Description =
                    $"Credit sale {sale.LocalInvoiceNumber}",

                TransactionDateUtc =
                    sale.SaleDateUtc,

                SyncStatus =
                    SyncQueueStatus.Pending,

                CreatedAtUtc =
                    now
            };

        _db.CustomerTransactions.Add(
            customerTransaction);
    }

    private static void PreparePendingHeader(
        LocalSale sale,
        Guid tenantId,
        Guid localCashSessionId,
        Guid? serverCashSessionId)
    {
        var now =
            DateTime.UtcNow;

        if (sale.Id == Guid.Empty)
        {
            sale.Id =
                Guid.NewGuid();
        }

        if (sale.ClientOperationId == Guid.Empty)
        {
            sale.ClientOperationId =
                Guid.NewGuid();
        }

        if (string.IsNullOrWhiteSpace(
                sale.LocalInvoiceNumber))
        {
            sale.LocalInvoiceNumber =
                GenerateLocalInvoiceNumber(now);
        }

        sale.TenantId =
            tenantId;

        sale.ServerId =
            null;

        sale.ServerInvoiceNumber =
            null;

        sale.LocalCashSessionId =
            localCashSessionId;

        sale.CashSessionServerId =
            serverCashSessionId;

        sale.Status =
            PendingSaleStatus;

        sale.PaymentStatus =
            LocalPaymentStatus.Unpaid;

        sale.SyncStatus =
            SyncQueueStatus.Draft;

        sale.SaleDateUtc =
            sale.SaleDateUtc == default
                ? now
                : EnsureUtc(
                    sale.SaleDateUtc);

        sale.CreatedAtUtc =
            sale.CreatedAtUtc == default
                ? now
                : EnsureUtc(
                    sale.CreatedAtUtc);

        sale.LastSyncedAtUtc =
            null;

        sale.PaidAmount =
            0m;

        sale.ChangeAmount =
            0m;

        sale.Payments.Clear();

        foreach (var line in sale.Lines)
        {
            if (line.Id == Guid.Empty)
            {
                line.Id =
                    Guid.NewGuid();
            }

            line.TenantId =
                tenantId;

            line.LocalSaleId =
                sale.Id;

            line.LocalSale =
                sale;

            line.Quantity =
                RoundQuantity(
                    line.Quantity);

            line.UnitQuantity =
                RoundQuantity(
                    line.UnitQuantity);

            line.UnitsPerPack =
                RoundQuantity(
                    line.UnitsPerPack <= 0m
                        ? 1m
                        : line.UnitsPerPack);

            line.UnitPrice =
                RoundMoney(
                    line.UnitPrice);

            line.VatRate =
                RoundPercentage(
                    line.VatRate);

            line.DiscountPercent =
                RoundPercentage(
                    line.DiscountPercent);

            line.DiscountAmount =
                RoundMoney(
                    line.DiscountAmount);

            line.UnitCostPrice =
                RoundMoney(
                    line.UnitCostPrice);
        }
    }

    private static void CopyPendingHeaderValues(
        LocalSale source,
        LocalSale target)
    {
        target.CustomerLocalId =
            source.CustomerLocalId;

        target.CustomerServerId =
            source.CustomerServerId;

        target.SaleDateUtc =
            source.SaleDateUtc;

        target.Notes =
            source.Notes;
    }

    private static LocalSaleLine CloneSaleLine(
        LocalSaleLine source)
    {
        return new LocalSaleLine
        {
            Id =
                Guid.NewGuid(),

            ProductLocalId =
                source.ProductLocalId,

            ProductServerId =
                source.ProductServerId,

            ProductName =
                source.ProductName,

            ProductBarcode =
                source.ProductBarcode,

            Quantity =
                source.Quantity,

            UnitProductLocalId =
                source.UnitProductLocalId,

            UnitProductServerId =
                source.UnitProductServerId,

            UnitQuantity =
                source.UnitQuantity,

            IsPack =
                source.IsPack,

            UnitsPerPack =
                source.UnitsPerPack,

            UnitPrice =
                source.UnitPrice,

            VatRate =
                source.VatRate,

            DiscountPercent =
                source.DiscountPercent,

            DiscountAmount =
                source.DiscountAmount,

            UnitCostPrice =
                source.UnitCostPrice,

            Notes =
                source.Notes
        };
    }

    private static LocalPayment ClonePayment(
        LocalPayment source)
    {
        return new LocalPayment
        {
            Id =
                Guid.NewGuid(),

            ServerId =
                null,

            ServerSaleId =
                null,

            Amount =
                source.Amount,

            Method =
                source.Method,

            TransactionRef =
                source.TransactionRef,

            CardLastFourDigits =
                source.CardLastFourDigits,

            PaidAtUtc =
                source.PaidAtUtc,

            Notes =
                source.Notes,

            SyncStatus =
                SyncQueueStatus.Pending,

            LastSyncedAtUtc =
                null
        };
    }

    private sealed class LocalSaleStockResolution
    {
        public required LocalSaleLine Line
        {
            get;
            init;
        }

        public Guid StockProductLocalId
        {
            get;
            init;
        }

        public Guid StockProductServerId
        {
            get;
            init;
        }

        public decimal StockQuantity
        {
            get;
            init;
        }
    }
}