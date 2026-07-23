using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Dto.Suppliers.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services;

public sealed class LocalSupplierService
    : ILocalSupplierService
{
    private const string SupplierEntityName = "Supplier";

    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public LocalSupplierService(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<SupplierResult> CreateAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        ValidateRequest(
            request.Name,
            request.PaymentTermsDays);

        var normalizedName =
            request.Name.Trim();

        var exists =
            await _db.Suppliers
                .AsNoTracking()
                .AnyAsync(
                    supplier =>
                        supplier.TenantId == tenantId &&
                        !supplier.IsDeleted &&
                        supplier.Name.ToLower() ==
                        normalizedName.ToLower(),
                    cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                $"Supplier '{normalizedName}' already exists locally.");
        }

        var now =
            DateTime.UtcNow;

        var supplier =
            new LocalSupplier
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ServerId = null,

                Name = normalizedName,
                ContactPerson =
                    NormalizeNullable(request.ContactPerson),
                Email =
                    NormalizeNullable(request.Email),
                Phone =
                    NormalizeNullable(request.Phone),
                Address =
                    NormalizeNullable(request.Address),
                City =
                    NormalizeNullable(request.City),
                PostalCode =
                    NormalizeNullable(request.PostalCode),
                Country =
                    NormalizeNullable(request.Country),
                TaxNumber =
                    NormalizeNullable(request.TaxNumber),
                BankAccount =
                    NormalizeNullable(request.BankAccount),

                PaymentTermsDays =
                    request.PaymentTermsDays,

                CurrentBalance = 0m,

                IsActive =
                    request.IsActive,

                IsDeleted = false,

                Notes =
                    NormalizeNullable(request.Notes),

                SyncStatus =
                    SyncQueueStatus.Pending,

                CreatedAtUtc = now
            };

        _db.Suppliers.Add(supplier);

        await AddOrMergeQueueItemAsync(
            tenantId,
            supplier.Id,
            SyncOperation.Create,
            cancellationToken);

        await _db.SaveChangesAsync(
            cancellationToken);

        return ToResult(supplier);
    }

    public async Task<SupplierResult> UpdateAsync(
        Guid id,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        ValidateRequest(
            request.Name,
            request.PaymentTermsDays);

        var supplier =
            await _db.Suppliers
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == id &&
                        !item.IsDeleted,
                    cancellationToken);

        if (supplier == null)
        {
            throw new InvalidOperationException(
                "Supplier not found locally.");
        }

        var normalizedName =
            request.Name.Trim();

        var exists =
            await _db.Suppliers
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id != id &&
                        !item.IsDeleted &&
                        item.Name.ToLower() ==
                        normalizedName.ToLower(),
                    cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                $"Supplier '{normalizedName}' already exists locally.");
        }

        supplier.Name =
            normalizedName;

        supplier.ContactPerson =
            NormalizeNullable(request.ContactPerson);

        supplier.Email =
            NormalizeNullable(request.Email);

        supplier.Phone =
            NormalizeNullable(request.Phone);

        supplier.Address =
            NormalizeNullable(request.Address);

        supplier.City =
            NormalizeNullable(request.City);

        supplier.PostalCode =
            NormalizeNullable(request.PostalCode);

        supplier.Country =
            NormalizeNullable(request.Country);

        supplier.TaxNumber =
            NormalizeNullable(request.TaxNumber);

        supplier.PaymentTermsDays =
            request.PaymentTermsDays;

        supplier.BankAccount =
            NormalizeNullable(request.BankAccount);

        supplier.IsActive =
            request.IsActive;

        supplier.Notes =
            NormalizeNullable(request.Notes);

        supplier.ModifiedAtUtc =
            DateTime.UtcNow;

        supplier.SyncStatus =
            SyncQueueStatus.Pending;

        await AddOrMergeQueueItemAsync(
            tenantId,
            supplier.Id,
            SyncOperation.Update,
            cancellationToken);

        await _db.SaveChangesAsync(
            cancellationToken);

        return ToResult(supplier);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var supplier =
            await _db.Suppliers
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == id &&
                        !item.IsDeleted,
                    cancellationToken);

        if (supplier == null)
        {
            throw new InvalidOperationException(
                "Supplier not found locally.");
        }

        var now =
            DateTime.UtcNow;

        supplier.IsDeleted = true;
        supplier.IsActive = false;
        supplier.DeletedAtUtc = now;
        supplier.ModifiedAtUtc = now;

        if (!supplier.ServerId.HasValue ||
            supplier.ServerId.Value == Guid.Empty)
        {
            /*
             * Le fournisseur n'a jamais été enregistré
             * dans la base centrale.
             */
            var pendingItems =
                await _db.SyncQueueItems
                    .Where(item =>
                        item.TenantId == tenantId &&
                        item.EntityName ==
                            SupplierEntityName &&
                        item.LocalEntityId ==
                            supplier.Id &&
                        item.Status !=
                            SyncQueueStatus.Done)
                    .ToListAsync(cancellationToken);

            _db.SyncQueueItems.RemoveRange(
                pendingItems);

            supplier.SyncStatus =
                SyncQueueStatus.Done;
        }
        else
        {
            supplier.SyncStatus =
                SyncQueueStatus.Pending;

            await AddOrMergeQueueItemAsync(
                tenantId,
                supplier.Id,
                SyncOperation.Delete,
                cancellationToken);
        }

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<PagedResult<SupplierResult>> QueryAsync(
        SupplierQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var page =
            query.Page <= 0
                ? 1
                : query.Page;

        var pageSize =
            Math.Clamp(
                query.PageSize <= 0
                    ? 10
                    : query.PageSize,
                1,
                100);

        var suppliers =
            _db.Suppliers
                .AsNoTracking()
                .Where(supplier =>
                    supplier.TenantId == tenantId &&
                    !supplier.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search =
                query.Search
                    .Trim()
                    .ToLower();

            suppliers =
                suppliers.Where(supplier =>
                    supplier.Name
                        .ToLower()
                        .Contains(search) ||

                    supplier.ContactPerson != null &&
                    supplier.ContactPerson
                        .ToLower()
                        .Contains(search) ||

                    supplier.Email != null &&
                    supplier.Email
                        .ToLower()
                        .Contains(search) ||

                    supplier.Phone != null &&
                    supplier.Phone
                        .ToLower()
                        .Contains(search) ||

                    supplier.City != null &&
                    supplier.City
                        .ToLower()
                        .Contains(search));
        }

        suppliers =
            query.SortBy?
                .Trim()
                .ToLowerInvariant() switch
            {
                "name" =>
                    query.Desc
                        ? suppliers.OrderByDescending(
                            supplier => supplier.Name)
                        : suppliers.OrderBy(
                            supplier => supplier.Name),

                "currentbalance" =>
                    query.Desc
                        ? suppliers.OrderByDescending(
                            supplier =>
                                supplier.CurrentBalance)
                        : suppliers.OrderBy(
                            supplier =>
                                supplier.CurrentBalance),

                "city" =>
                    query.Desc
                        ? suppliers.OrderByDescending(
                            supplier => supplier.City)
                        : suppliers.OrderBy(
                            supplier => supplier.City),

                _ =>
                    query.Desc
                        ? suppliers.OrderByDescending(
                            supplier =>
                                supplier.CreatedAtUtc)
                        : suppliers.OrderBy(
                            supplier =>
                                supplier.CreatedAtUtc)
            };

        var total =
            await suppliers.CountAsync(
                cancellationToken);

        var items =
            await suppliers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        return new PagedResult<SupplierResult>
        {
            Items = items
                .Select(ToResult)
                .ToList(),

            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<SupplierResult>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var suppliers =
            await _db.Suppliers
                .AsNoTracking()
                .Where(supplier =>
                    supplier.TenantId == tenantId &&
                    !supplier.IsDeleted &&
                    supplier.IsActive)
                .OrderBy(supplier =>
                    supplier.Name)
                .ToListAsync(cancellationToken);

        return suppliers
            .Select(ToResult)
            .ToList();
    }

    private async Task AddOrMergeQueueItemAsync(
        Guid tenantId,
        Guid localSupplierId,
        string requestedOperation,
        CancellationToken cancellationToken)
    {
        var pendingItems =
            await _db.SyncQueueItems
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.EntityName ==
                        SupplierEntityName &&
                    item.LocalEntityId ==
                        localSupplierId &&
                    item.Status !=
                        SyncQueueStatus.Done)
                .OrderBy(item =>
                    item.CreatedAtUtc)
                .ToListAsync(cancellationToken);

        var pendingCreate =
            pendingItems.FirstOrDefault(item =>
                item.Operation ==
                SyncOperation.Create);

        var pendingUpdate =
            pendingItems.FirstOrDefault(item =>
                item.Operation ==
                SyncOperation.Update);

        var pendingDelete =
            pendingItems.FirstOrDefault(item =>
                item.Operation ==
                SyncOperation.Delete);

        if (requestedOperation == SyncOperation.Create)
        {
            if (pendingCreate != null)
                return;

            _db.SyncQueueItems.Add(
                CreateQueueItem(
                    tenantId,
                    localSupplierId,
                    SyncOperation.Create));

            return;
        }

        if (requestedOperation == SyncOperation.Update)
        {
            /*
             * Une création en attente utilisera la version actuelle
             * de LocalSupplier. Aucun Update supplémentaire.
             */
            if (pendingCreate != null ||
                pendingUpdate != null ||
                pendingDelete != null)
            {
                return;
            }

            _db.SyncQueueItems.Add(
                CreateQueueItem(
                    tenantId,
                    localSupplierId,
                    SyncOperation.Update));

            return;
        }

        if (requestedOperation == SyncOperation.Delete)
        {
            if (pendingDelete != null)
                return;

            var obsoleteUpdates =
                pendingItems.Where(item =>
                    item.Operation ==
                    SyncOperation.Update);

            _db.SyncQueueItems.RemoveRange(
                obsoleteUpdates);

            _db.SyncQueueItems.Add(
                CreateQueueItem(
                    tenantId,
                    localSupplierId,
                    SyncOperation.Delete));

            return;
        }

        throw new InvalidOperationException(
            $"Unsupported Supplier sync operation " +
            $"'{requestedOperation}'.");
    }

    private static SyncQueueItem CreateQueueItem(
        Guid tenantId,
        Guid localSupplierId,
        string operation)
    {
        return new SyncQueueItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,

            ClientOperationId =
                Guid.NewGuid(),

            LocalEntityId =
                localSupplierId,

            EntityName =
                SupplierEntityName,

            Operation =
                operation,

            Status =
                SyncQueueStatus.Pending,

            Attempts = 0,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static void ValidateRequest(
        string? name,
        int paymentTermsDays)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Supplier name is required.");
        }

        if (name.Trim().Length > 200)
        {
            throw new InvalidOperationException(
                "Supplier name cannot exceed 200 characters.");
        }

        if (paymentTermsDays < 0)
        {
            throw new InvalidOperationException(
                "Payment terms cannot be negative.");
        }

        if (paymentTermsDays > 3650)
        {
            throw new InvalidOperationException(
                "Payment terms are too large.");
        }
    }

    private static SupplierResult ToResult(
        LocalSupplier supplier)
    {
        return new SupplierResult
        {
            /*
             * La page locale utilise l'identifiant SQLite.
             */
            Id = supplier.Id,

            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            City = supplier.City,
            PostalCode = supplier.PostalCode,
            Country = supplier.Country,
            TaxNumber = supplier.TaxNumber,
            PaymentTermsDays = supplier.PaymentTermsDays,
            BankAccount = supplier.BankAccount,
            IsActive = supplier.IsActive,
            Notes = supplier.Notes
        };
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}