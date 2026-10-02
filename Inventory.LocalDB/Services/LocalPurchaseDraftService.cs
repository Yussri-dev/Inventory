using Inventory.LocalDB.Context;
using Inventory.LocalDB.Enums;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.LocalDB.Services;

public sealed class LocalPurchaseDraftService
    : ILocalPurchaseDraftService
{
    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;
    private readonly ILogger<LocalPurchaseDraftService> _logger;

    /*
     * Empêche deux sauvegardes automatiques d'utiliser simultanément
     * le même DbContext SQLite.
     */
    private readonly SemaphoreSlim _mutationGate =
        new(1, 1);

    public LocalPurchaseDraftService(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext,
        ILogger<LocalPurchaseDraftService> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<LocalPurchaseDraft?> GetActiveAsync()
    {
        await _mutationGate.WaitAsync();

        try
        {
            DetachPurchaseDraftEntities();

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            return await _db.PurchaseDrafts
                .AsNoTracking()
                .Include(draft => draft.Lines)
                    .ThenInclude(line => line.Adjustments)
                .Where(draft =>
                    draft.TenantId == tenantId &&
                    (draft.Status == PurchaseDraftStatus.Active ||
                     draft.Status == PurchaseDraftStatus.Suspended))
                .OrderByDescending(draft => draft.UpdatedAtUtc)
                .FirstOrDefaultAsync();
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    public async Task<Guid> SaveAsync(
    SaveLocalPurchaseDraftRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        await _mutationGate.WaitAsync();

        try
        {
            DetachPurchaseDraftEntities();

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            await ValidateReferencesAsync(
                tenantId,
                request);

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            LocalPurchaseDraft? draft;

            if (request.DraftId.HasValue)
            {
                draft = await _db.PurchaseDrafts
                    .SingleOrDefaultAsync(current =>
                        current.Id == request.DraftId.Value &&
                        current.TenantId == tenantId);

                if (draft == null)
                {
                    throw new KeyNotFoundException(
                        $"Purchase draft '{request.DraftId.Value}' was not found.");
                }
            }
            else
            {
                draft = await _db.PurchaseDrafts
                    .Where(current =>
                        current.TenantId == tenantId &&
                        (
                            current.Status == PurchaseDraftStatus.Active ||
                            current.Status == PurchaseDraftStatus.Suspended
                        ))
                    .OrderByDescending(current =>
                        current.UpdatedAtUtc)
                    .FirstOrDefaultAsync();
            }

            var now = DateTime.UtcNow;

            if (draft == null)
            {
                draft = new LocalPurchaseDraft
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SupplierLocalId = request.SupplierLocalId,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Status = PurchaseDraftStatus.Active,
                    Lines = new List<LocalPurchaseDraftLine>()
                };

                _db.PurchaseDrafts.Add(draft);
            }
            else
            {
                /*
                 * Delete directly in SQLite.
                 *
                 * We intentionally do not load the old Lines/Adjustments
                 * into EF's ChangeTracker.
                 *
                 * Adjustments are removed by ON DELETE CASCADE.
                 */
                await _db.PurchaseDraftLines
                    .Where(line =>
                        line.PurchaseDraftId == draft.Id)
                    .ExecuteDeleteAsync();

                draft.SupplierLocalId =
                    request.SupplierLocalId;

                draft.UpdatedAtUtc = now;
                draft.Status = PurchaseDraftStatus.Active;
            }

            foreach (var lineRequest in
                     request.Lines
                         .OrderBy(line =>
                             line.DisplayOrder))
            {
                var orderedAdjustments =
                    lineRequest.Adjustments
                        .OrderBy(adjustment =>
                            adjustment.DisplayOrder)
                        .ToList();

                var effectiveUnitPrice =
                    CalculateEffectiveUnitPrice(
                        lineRequest.BasePurchasePrice,
                        orderedAdjustments);

                var lineId =
                    Guid.NewGuid();

                var line =
                    new LocalPurchaseDraftLine
                    {
                        Id = lineId,

                        PurchaseDraftId =
                            draft.Id,

                        ProductLocalId =
                            lineRequest.ProductLocalId,

                        Quantity =
                            RoundQuantity(
                                lineRequest.Quantity),

                        BasePurchasePrice =
                            RoundMoney(
                                lineRequest.BasePurchasePrice),

                        EffectiveUnitPrice =
                            effectiveUnitPrice,

                        VatRate =
                            Math.Round(
                                lineRequest.VatRate,
                                2,
                                MidpointRounding.AwayFromZero),

                        DisplayOrder =
                            lineRequest.DisplayOrder,

                        Adjustments =
                            new List<LocalPurchaseDraftAdjustment>()
                    };

                foreach (var adjustmentRequest in
                         orderedAdjustments)
                {
                    line.Adjustments.Add(
                        new LocalPurchaseDraftAdjustment
                        {
                            Id = Guid.NewGuid(),

                            PurchaseDraftLineId =
                                lineId,

                            Type =
                                adjustmentRequest.Type,

                            Value =
                                RoundAdjustmentValue(
                                    adjustmentRequest.Type,
                                    adjustmentRequest.Value),

                            DisplayOrder =
                                adjustmentRequest.DisplayOrder
                        });
                }

                _db.PurchaseDraftLines.Add(line);
            }

            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogDebug(
                "Purchase draft {DraftId} saved with {LineCount} lines.",
                draft.Id,
                request.Lines.Count);

            DetachPurchaseDraftEntities();

            return draft.Id;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            foreach (var entry in exception.Entries)
            {
                _logger.LogError(
                    "Concurrency conflict while saving purchase draft. " +
                    "Entity={Entity}, State={State}",
                    entry.Metadata.ClrType.Name,
                    entry.State);
            }

            DetachPurchaseDraftEntities();

            throw;
        }
        catch
        {
            DetachPurchaseDraftEntities();
            throw;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    private static decimal CalculateEffectiveUnitPrice(
    decimal basePrice,
    IEnumerable<SaveLocalPurchaseDraftAdjustmentRequest> adjustments)
    {
        var price =
            RoundMoney(basePrice);

        foreach (var adjustment in adjustments)
        {
            var value =
                RoundAdjustmentValue(
                    adjustment.Type,
                    adjustment.Value);

            price =
                adjustment.Type switch
                {
                    PurchaseDraftAdjustmentType.PriceOverride =>
                        value,

                    PurchaseDraftAdjustmentType.DiscountPercent =>
                        price *
                        (1m - value / 100m),

                    PurchaseDraftAdjustmentType.DiscountAmount =>
                        price - value,

                    PurchaseDraftAdjustmentType.Fee =>
                        price + value,

                    _ =>
                        throw new InvalidOperationException(
                            $"Unsupported adjustment type: {adjustment.Type}.")
                };

            if (price < 0m)
            {
                throw new InvalidOperationException(
                    "Purchase adjustments cannot produce a negative price.");
            }

            price =
                RoundMoney(price);
        }

        return price;
    }

    public async Task SuspendAsync(Guid draftId)
    {
        await _mutationGate.WaitAsync();

        try
        {
            DetachPurchaseDraftEntities();

            var tenantId = _tenantContext.GetRequiredTenantId();

            var affectedRows = await _db.PurchaseDrafts
        .Where(draft =>
            draft.Id == draftId &&
            draft.TenantId == tenantId)
        .ExecuteUpdateAsync(update => update
            .SetProperty(
                draft => draft.Status,
                PurchaseDraftStatus.Suspended)
            .SetProperty(
                draft => draft.UpdatedAtUtc,
                DateTime.UtcNow));

            if (affectedRows == 0)
            {
                throw new KeyNotFoundException(
                    $"Purchase draft '{draftId}' was not found.");
            }

            DetachPurchaseDraftEntities();
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    public async Task DeleteAsync(Guid draftId)
    {
        await _mutationGate.WaitAsync();

        try
        {
            DetachPurchaseDraftEntities();

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            await _db.PurchaseDrafts
                .Where(draft =>
                    draft.Id == draftId &&
                    draft.TenantId == tenantId)
                .ExecuteDeleteAsync();

            DetachPurchaseDraftEntities();
        }
        catch
        {
            DetachPurchaseDraftEntities();
            throw;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    private async Task ValidateReferencesAsync(
        Guid tenantId,
        SaveLocalPurchaseDraftRequest request)
    {
        if (request.SupplierLocalId.HasValue)
        {
            var supplierExists =
                await _db.Suppliers.AnyAsync(supplier =>
                    supplier.Id ==
                        request.SupplierLocalId.Value &&
                    supplier.TenantId == tenantId);

            if (!supplierExists)
            {
                throw new InvalidOperationException(
                    "The selected supplier does not exist locally.");
            }
        }

        var requestedProductIds =
            request.Lines
                .Select(line =>
                    line.ProductLocalId)
                .Distinct()
                .ToList();

        if (requestedProductIds.Count == 0)
        {
            return;
        }

        var existingProductIds =
            await _db.Products
                .Where(product =>
                    product.TenantId == tenantId &&
                    requestedProductIds.Contains(
                        product.Id))
                .Select(product =>
                    product.Id)
                .ToListAsync();

        var missingProductIds =
            requestedProductIds
                .Except(existingProductIds)
                .ToList();

        if (missingProductIds.Count > 0)
        {
            throw new InvalidOperationException(
                "One or more draft products do not exist locally.");
        }
    }

    private static void ValidateRequest(
        SaveLocalPurchaseDraftRequest request)
    {
        var duplicateProduct =
            request.Lines
                .GroupBy(line =>
                    line.ProductLocalId)
                .FirstOrDefault(group =>
                    group.Count() > 1);

        if (duplicateProduct != null)
        {
            throw new InvalidOperationException(
                $"Product '{duplicateProduct.Key}' appears multiple times.");
        }

        foreach (var line in request.Lines)
        {
            if (line.ProductLocalId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A draft line contains an invalid product ID.");
            }

            if (line.Quantity <= 0m)
            {
                throw new InvalidOperationException(
                    "Draft quantities must be greater than zero.");
            }

            if (line.BasePurchasePrice < 0m)
            {
                throw new InvalidOperationException(
                    "Purchase prices cannot be negative.");
            }

            if (line.VatRate < 0m ||
                line.VatRate > 100m)
            {
                throw new InvalidOperationException(
                    "VAT rates must be between 0 and 100.");
            }

            foreach (var adjustment in line.Adjustments)
            {
                ValidateAdjustment(
                    adjustment);
            }
        }
    }

    private static void ValidateAdjustment(
        SaveLocalPurchaseDraftAdjustmentRequest adjustment)
    {
        var isValid =
            adjustment.Type switch
            {
                PurchaseDraftAdjustmentType.PriceOverride =>
                    adjustment.Value > 0m,

                PurchaseDraftAdjustmentType.DiscountPercent =>
                    adjustment.Value > 0m &&
                    adjustment.Value <= 100m,

                PurchaseDraftAdjustmentType.DiscountAmount =>
                    adjustment.Value > 0m,

                PurchaseDraftAdjustmentType.Fee =>
                    adjustment.Value > 0m,

                _ => false
            };

        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Invalid purchase draft adjustment: {adjustment.Type}.");
        }
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

    private static decimal RoundAdjustmentValue(
        PurchaseDraftAdjustmentType type,
        decimal value)
    {
        return type ==
            PurchaseDraftAdjustmentType.DiscountPercent
                ? Math.Round(
                    value,
                    2,
                    MidpointRounding.AwayFromZero)
                : RoundMoney(value);
    }

    private void DetachPurchaseDraftEntities()
    {
        var entries = _db.ChangeTracker
            .Entries()
            .Where(entry =>
                entry.Entity is LocalPurchaseDraft ||
                entry.Entity is LocalPurchaseDraftLine ||
                entry.Entity is LocalPurchaseDraftAdjustment)
            .ToList();

        foreach (var entry in entries)
        {
            entry.State = EntityState.Detached;
        }
    }
}