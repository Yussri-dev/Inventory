using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.LocalDB.Services
{
    public sealed class LocalInventorySessionService
     : ILocalInventorySessionService
    {
        private const string StockMovementEntityName =
            "StockMovement";

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public LocalInventorySessionService(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<IReadOnlyList<LocalInventorySession>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            return await _db.InventorySessions
                .AsNoTracking()
                .Where(session =>
                    session.TenantId == tenantId)
                .OrderByDescending(session =>
                    session.StartedAtUtc)
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<LocalInventorySession?>
            GetByIdAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default)
        {
            ValidateRequiredId(
                sessionId,
                nameof(sessionId));

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            return await _db.InventorySessions
                .AsNoTracking()
                .Include(session =>
                    session.Lines)
                .FirstOrDefaultAsync(
                    session =>
                        session.TenantId == tenantId &&
                        session.Id == sessionId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<LocalInventoryLine>>
            GetLinesAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default)
        {
            ValidateRequiredId(
                sessionId,
                nameof(sessionId));

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            return await _db.InventoryLines
                .AsNoTracking()
                .Where(line =>
                    line.TenantId == tenantId &&
                    line.LocalInventorySessionId ==
                        sessionId)
                .OrderBy(line =>
                    line.ProductName)
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<LocalInventorySession>
            CreateAsync(
                string sessionNumber,
                string? notes = null,
                CancellationToken cancellationToken = default)
        {
            var lockAcquired =
                await LocalDatabaseWriteGate.Semaphore
                    .WaitAsync(
                        0,
                        cancellationToken);

            if (!lockAcquired)
            {
                throw new InvalidOperationException(
                    "Another local database operation is running.");
            }

            try
            {
                var tenantId =
                    _tenantContext.GetRequiredTenantId();

                sessionNumber =
                    NormalizeSessionNumber(
                        sessionNumber);

                var activeSessionExists =
                    await _db.InventorySessions
                        .AsNoTracking()
                        .AnyAsync(
                            session =>
                                session.TenantId == tenantId &&
                                session.Status ==
                                    LocalInventoryStatus.InProgress,
                            cancellationToken);

                if (activeSessionExists)
                {
                    throw new InvalidOperationException(
                        "An inventory session is already in progress.");
                }

                var numberExists =
                    await _db.InventorySessions
                        .AsNoTracking()
                        .AnyAsync(
                            session =>
                                session.TenantId == tenantId &&
                                session.SessionNumber ==
                                    sessionNumber,
                            cancellationToken);

                if (numberExists)
                {
                    throw new InvalidOperationException(
                        $"Inventory session '{sessionNumber}' already exists.");
                }

                var now =
                    DateTime.UtcNow;

                var session =
                    new LocalInventorySession
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        ClientOperationId =
                            Guid.NewGuid(),

                        ServerId =
                            null,

                        SessionNumber =
                            sessionNumber,

                        Status =
                            LocalInventoryStatus.InProgress,

                        Notes =
                            NormalizeNullable(
                                notes),

                        StartedAtUtc =
                            now,

                        ClosedAtUtc =
                            null,

                        ValidatedAtUtc =
                            null,

                        SyncStatus =
                            SyncQueueStatus.Pending,

                        CreatedAtUtc =
                            now
                    };

                _db.InventorySessions.Add(
                    session);

                await _db.SaveChangesAsync(
                    cancellationToken);

                return session;
            }
            finally
            {
                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        public async Task<LocalInventoryLine>
            AddLineAsync(
                Guid sessionId,
                Guid productLocalId,
                decimal countedQuantity,
                string? notes = null,
                CancellationToken cancellationToken = default)
        {
            ValidateRequiredId(
                sessionId,
                nameof(sessionId));

            ValidateRequiredId(
                productLocalId,
                nameof(productLocalId));

            ValidateCountedQuantity(
                countedQuantity);

            await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                cancellationToken);

            try
            {
                var tenantId =
                    _tenantContext.GetRequiredTenantId();

                var session =
                    await GetTrackedSessionAsync(
                        tenantId,
                        sessionId,
                        includeLines: false,
                        cancellationToken);

                EnsureSessionInProgress(
                    session);

                var duplicateExists =
                    await _db.InventoryLines
                        .AsNoTracking()
                        .AnyAsync(
                            line =>
                                line.TenantId == tenantId &&
                                line.LocalInventorySessionId ==
                                    sessionId &&
                                line.ProductLocalId ==
                                    productLocalId,
                            cancellationToken);

                if (duplicateExists)
                {
                    throw new InvalidOperationException(
                        "This product already exists in the inventory session.");
                }

                var product =
                    await _db.Products
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId &&
                                item.Id == productLocalId &&
                                !item.IsDeletedLocally,
                            cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Local product '{productLocalId}' was not found.");

                var stock =
                    await _db.Stocks
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId &&
                                item.ProductLocalId ==
                                    productLocalId,
                            cancellationToken);

                var now =
                    DateTime.UtcNow;

                var line =
                    new LocalInventoryLine
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        LocalInventorySessionId =
                            session.Id,

                        ProductLocalId =
                            product.Id,

                        ProductServerId =
                            product.ServerId,

                        ProductName =
                            product.Name,

                        ProductBarcode =
                            product.Barcode,

                        ExpectedQuantity =
                            RoundQuantity(
                                stock?.Quantity ??
                                product.LocalStockQuantity),

                        CountedQuantity =
                            RoundQuantity(
                                countedQuantity),

                        IsAdjusted =
                            false,

                        Notes =
                            NormalizeNullable(
                                notes),

                        CreatedAtUtc =
                            now
                    };

                _db.InventoryLines.Add(
                    line);

                session.SyncStatus =
                    SyncQueueStatus.Pending;

                await _db.SaveChangesAsync(
                    cancellationToken);

                return line;
            }
            finally
            {
                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        public async Task<LocalInventoryLine>
            UpdateLineAsync(
                Guid lineId,
                decimal countedQuantity,
                string? notes = null,
                CancellationToken cancellationToken = default)
        {
            ValidateRequiredId(
                lineId,
                nameof(lineId));

            ValidateCountedQuantity(
                countedQuantity);

            await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                cancellationToken);

            try
            {
                var tenantId =
                    _tenantContext.GetRequiredTenantId();

                var line =
                    await _db.InventoryLines
                        .Include(item =>
                            item.Session)
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId &&
                                item.Id == lineId,
                            cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Local inventory line '{lineId}' was not found.");

                EnsureSessionInProgress(
                    line.Session);

                if (line.IsAdjusted)
                {
                    throw new InvalidOperationException(
                        "An adjusted inventory line cannot be modified.");
                }

                line.CountedQuantity =
                    RoundQuantity(
                        countedQuantity);

                line.Notes =
                    NormalizeNullable(
                        notes);

                line.Session.SyncStatus =
                    SyncQueueStatus.Pending;

                await _db.SaveChangesAsync(
                    cancellationToken);

                return line;
            }
            finally
            {
                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        public async Task CloseAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            ValidateRequiredId(
                sessionId,
                nameof(sessionId));

            await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                cancellationToken);

            try
            {
                var tenantId =
                    _tenantContext.GetRequiredTenantId();

                var session =
                    await GetTrackedSessionAsync(
                        tenantId,
                        sessionId,
                        includeLines: true,
                        cancellationToken);

                if (session.Status ==
                        LocalInventoryStatus.Completed ||
                    session.Status ==
                        LocalInventoryStatus.Validated)
                {
                    return;
                }

                EnsureSessionInProgress(
                    session);

                if (session.Lines.Count == 0)
                {
                    throw new InvalidOperationException(
                        "The inventory session contains no lines.");
                }

                session.Status =
                    LocalInventoryStatus.Completed;

                session.ClosedAtUtc =
                    DateTime.UtcNow;

                session.SyncStatus =
                    SyncQueueStatus.Pending;

                await _db.SaveChangesAsync(
                    cancellationToken);
            }
            finally
            {
                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        public async Task ValidateAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            ValidateRequiredId(
                sessionId,
                nameof(sessionId));

            await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                cancellationToken);

            try
            {
                var tenantId =
                    _tenantContext.GetRequiredTenantId();

                await using var transaction =
                    await _db.Database.BeginTransactionAsync(
                        cancellationToken);

                try
                {
                    var session =
                        await GetTrackedSessionAsync(
                            tenantId,
                            sessionId,
                            includeLines: true,
                            cancellationToken);

                    if (session.Status ==
                        LocalInventoryStatus.Validated)
                    {
                        await transaction.CommitAsync(
                            cancellationToken);

                        return;
                    }

                    if (session.Status !=
                        LocalInventoryStatus.Completed)
                    {
                        throw new InvalidOperationException(
                            "The inventory session must be closed before validation.");
                    }

                    if (session.Lines.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "The inventory session contains no lines.");
                    }

                    var duplicateProduct =
                        session.Lines
                            .GroupBy(line =>
                                line.ProductLocalId)
                            .FirstOrDefault(group =>
                                group.Count() > 1);

                    if (duplicateProduct != null)
                    {
                        throw new InvalidOperationException(
                            $"Multiple lines exist for local product " +
                            $"'{duplicateProduct.Key}'.");
                    }

                    var productLocalIds =
                        session.Lines
                            .Where(line =>
                                !line.IsAdjusted)
                            .Select(line =>
                                line.ProductLocalId)
                            .Distinct()
                            .ToList();

                    var products =
                        await _db.Products
                            .Where(product =>
                                product.TenantId == tenantId &&
                                productLocalIds.Contains(
                                    product.Id) &&
                                !product.IsDeletedLocally)
                            .ToListAsync(
                                cancellationToken);

                    var productsById =
                        products.ToDictionary(
                            product =>
                                product.Id);

                    var stocks =
                        await _db.Stocks
                            .Where(stock =>
                                stock.TenantId == tenantId &&
                                productLocalIds.Contains(
                                    stock.ProductLocalId))
                            .ToListAsync(
                                cancellationToken);

                    var duplicateStock =
                        stocks
                            .GroupBy(stock =>
                                stock.ProductLocalId)
                            .FirstOrDefault(group =>
                                group.Count() > 1);

                    if (duplicateStock != null)
                    {
                        throw new InvalidOperationException(
                            $"Multiple local stocks exist for product " +
                            $"'{duplicateStock.Key}'.");
                    }

                    var stocksByProductId =
                        stocks.ToDictionary(
                            stock =>
                                stock.ProductLocalId);

                    var now =
                        DateTime.UtcNow;

                    foreach (var line in session.Lines)
                    {
                        if (line.IsAdjusted)
                        {
                            continue;
                        }

                        ValidateCountedQuantity(
                            line.CountedQuantity);

                        if (!productsById.TryGetValue(
                                line.ProductLocalId,
                                out var product))
                        {
                            throw new KeyNotFoundException(
                                $"Local product '{line.ProductLocalId}' was not found.");
                        }

                        if (!stocksByProductId.TryGetValue(
                                product.Id,
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
                                        product.Id,

                                    ProductServerId =
                                        product.ServerId,

                                    ProductName =
                                        product.Name,

                                    ProductBarcode =
                                        product.Barcode,

                                    Quantity =
                                        0m,

                                    ReservedQuantity =
                                        0m,

                                    LastUpdatedUtc =
                                        now,

                                    LastSyncedAtUtc =
                                        null
                                };

                            _db.Stocks.Add(
                                stock);

                            stocksByProductId.Add(
                                product.Id,
                                stock);
                        }

                        var quantityBefore =
                            RoundQuantity(
                                stock.Quantity);

                        var quantityAfter =
                            RoundQuantity(
                                line.CountedQuantity);

                        var quantityChange =
                            RoundQuantity(
                                quantityAfter -
                                quantityBefore);

                        stock.Quantity =
                            quantityAfter;

                        stock.ProductServerId =
                            product.ServerId;

                        stock.ProductName =
                            product.Name;

                        stock.ProductBarcode =
                            product.Barcode;

                        stock.LastUpdatedUtc =
                            now;

                        product.LocalStockQuantity =
                            quantityAfter;

                        if (quantityChange != 0m)
                        {
                            CreateStockAdjustment(
                                tenantId,
                                session,
                                line,
                                product,
                                quantityBefore,
                                quantityAfter,
                                quantityChange,
                                now);
                        }

                        line.ProductServerId =
                            product.ServerId;

                        line.IsAdjusted =
                            true;
                    }

                    session.Status =
                        LocalInventoryStatus.Validated;

                    session.ValidatedAtUtc =
                        now;

                    session.SyncStatus =
                        SyncQueueStatus.Pending;

                    await _db.SaveChangesAsync(
                        cancellationToken);

                    await transaction.CommitAsync(
                        cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);

                    throw;
                }
            }
            finally
            {
                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        public async Task DeleteLineAsync(
            Guid lineId,
            CancellationToken cancellationToken = default)
        {
            ValidateRequiredId(
                lineId,
                nameof(lineId));

            await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                cancellationToken);

            try
            {
                var tenantId =
                    _tenantContext.GetRequiredTenantId();

                var line =
                    await _db.InventoryLines
                        .Include(item =>
                            item.Session)
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId &&
                                item.Id == lineId,
                            cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Local inventory line '{lineId}' was not found.");

                EnsureSessionInProgress(
                    line.Session);

                if (line.IsAdjusted)
                {
                    throw new InvalidOperationException(
                        "An adjusted line cannot be deleted.");
                }

                _db.InventoryLines.Remove(
                    line);

                line.Session.SyncStatus =
                    SyncQueueStatus.Pending;

                await _db.SaveChangesAsync(
                    cancellationToken);
            }
            finally
            {
                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        public async Task DeleteAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            ValidateRequiredId(
                sessionId,
                nameof(sessionId));

            await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                cancellationToken);

            try
            {
                var tenantId =
                    _tenantContext.GetRequiredTenantId();

                var session =
                    await GetTrackedSessionAsync(
                        tenantId,
                        sessionId,
                        includeLines: true,
                        cancellationToken);

                if (session.Status ==
                    LocalInventoryStatus.Validated)
                {
                    throw new InvalidOperationException(
                        "A validated inventory session cannot be deleted.");
                }

                _db.InventorySessions.Remove(
                    session);

                await _db.SaveChangesAsync(
                    cancellationToken);
            }
            finally
            {
                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        private async Task<LocalInventorySession>
            GetTrackedSessionAsync(
                Guid tenantId,
                Guid sessionId,
                bool includeLines,
                CancellationToken cancellationToken)
        {
            IQueryable<LocalInventorySession> query =
                _db.InventorySessions;

            if (includeLines)
            {
                query =
                    query.Include(session =>
                        session.Lines);
            }

            return await query
                .FirstOrDefaultAsync(
                    session =>
                        session.TenantId == tenantId &&
                        session.Id == sessionId,
                    cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Local inventory session '{sessionId}' was not found.");
        }

        private void CreateStockAdjustment(
            Guid tenantId,
            LocalInventorySession session,
            LocalInventoryLine line,
            LocalProduct product,
            decimal quantityBefore,
            decimal quantityAfter,
            decimal quantityChange,
            DateTime now)
        {
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
                        product.Id,

                    ProductServerId =
                        product.ServerId ??
                        Guid.Empty,

                    ProductName =
                        product.Name,

                    ProductBarcode =
                        product.Barcode,

                    QuantityBefore =
                        quantityBefore,

                    QuantityAfter =
                        quantityAfter,

                    QuantityChange =
                        quantityChange,

                    Type =
                        LocalStockMovementType.Adjustment,

                    UnitCost =
                        product.PurchasePrice,

                    LocalReferenceId =
                        session.Id,

                    ServerReferenceId =
                        session.ServerId,

                    ReferenceNumber =
                        session.SessionNumber,

                    Notes =
                        string.IsNullOrWhiteSpace(
                            line.Notes)
                            ? "Physical inventory adjustment."
                            : line.Notes.Trim(),

                    MovementDateUtc =
                        now,

                    SyncStatus =
                        SyncQueueStatus.Pending
                };

            _db.StockMovements.Add(
                movement);

            var queueItem =
                new SyncQueueItem
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    LocalEntityId =
                        movement.Id,

                    ClientOperationId =
                        movement.ClientOperationId,

                    EntityName =
                        StockMovementEntityName,

                    Operation =
                        SyncOperation.Create,

                    PayloadJson =
                        JsonSerializer.Serialize(
                            movement),

                    Status =
                        SyncQueueStatus.Pending,

                    Attempts =
                        0,

                    ErrorMessage =
                        null,

                    CreatedAtUtc =
                        now
                };

            _db.SyncQueueItems.Add(
                queueItem);
        }

        private static void EnsureSessionInProgress(
            LocalInventorySession session)
        {
            if (session.Status !=
                LocalInventoryStatus.InProgress)
            {
                throw new InvalidOperationException(
                    "Only an inventory session in progress can be modified.");
            }
        }

        private static void ValidateRequiredId(
            Guid value,
            string parameterName)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException(
                    "A non-empty identifier is required.",
                    parameterName);
            }
        }

        private static void ValidateCountedQuantity(
            decimal quantity)
        {
            if (quantity < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quantity),
                    "Counted quantity cannot be negative.");
            }
        }

        private static string NormalizeSessionNumber(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return
                    $"INV-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            }

            value =
                value.Trim();

            return value.Length <= 100
                ? value
                : value[..100];
        }

        private static string? NormalizeNullable(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }

            value =
                value.Trim();

            return value.Length <= 1000
                ? value
                : value[..1000];
        }

        private static decimal RoundQuantity(
            decimal value)
        {
            return Math.Round(
                value,
                3,
                MidpointRounding.AwayFromZero);
        }
    }
}
