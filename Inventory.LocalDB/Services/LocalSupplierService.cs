using Inventory.Dto.Suppliers.Requests;
using Inventory.Dto.Suppliers.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.LocalDB.Services;

public sealed class LocalSupplierService
    : ILocalSupplierService
{
    private const string SupplierEntityName = "Supplier";

    private const string CreateOperation = "Create";

    private const string UpdateOperation = "Update";

    private const string DeleteOperation = "Delete";

    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public LocalSupplierService(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<SupplierResult?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Supplier id is required.",
                nameof(id));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var supplier =
            await _db.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == id &&
                        !item.IsDeleted,
                    cancellationToken);

        return supplier == null
            ? null
            : ToResult(supplier);
    }

    public async Task<SupplierResult> CreateAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        ValidateName(request.Name);

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
                ServerId = null,
                TenantId = tenantId,

                Name = normalizedName,
                Email = NormalizeNullable(request.Email),
                Phone = NormalizeNullable(request.Phone),
                Address = NormalizeNullable(request.Address),
                TaxNumber = NormalizeNullable(request.TaxNumber),

                CurrentBalance = 0m,

                IsActive = request.IsActive,
                IsDeleted = false,

                Notes = NormalizeNullable(request.Notes),

                ContactPerson = NormalizeNullable(request.ContactPerson),

                City = NormalizeNullable(request.City),

                PostalCode = NormalizeNullable(request.PostalCode),

                Country = NormalizeNullable(request.Country),

                PaymentTermsDays = request.PaymentTermsDays,

                BankAccount = NormalizeNullable(request.BankAccount),

                SyncStatus = SyncQueueStatus.Pending,
                CreatedAtUtc = now
            };

        _db.Suppliers.Add(supplier);

        await AddOrMergeQueueItemAsync(
            tenantId,
            supplier,
            CreateOperation,
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

        ValidateName(request.Name);

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

        var nameExists =
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

        if (nameExists)
        {
            throw new InvalidOperationException(
                $"Supplier '{normalizedName}' already exists locally.");
        }

        supplier.Name =
            normalizedName;

        supplier.Email = NormalizeNullable(request.Email);

        supplier.Phone = NormalizeNullable(request.Phone);

        supplier.Address = NormalizeNullable(request.Address);

        supplier.TaxNumber = NormalizeNullable(request.TaxNumber);

        supplier.IsActive = request.IsActive;

        supplier.Notes = NormalizeNullable(request.Notes);

        supplier.ModifiedAtUtc = DateTime.UtcNow;

        supplier.SyncStatus = SyncQueueStatus.Pending;

        supplier.ContactPerson = NormalizeNullable(request.ContactPerson);

        supplier.City = NormalizeNullable(request.City);

        supplier.PostalCode = NormalizeNullable(request.PostalCode);

        supplier.Country = NormalizeNullable(request.Country);

        supplier.PaymentTermsDays = request.PaymentTermsDays;

        supplier.BankAccount = NormalizeNullable(request.BankAccount);

        await AddOrMergeQueueItemAsync(
            tenantId,
            supplier,
            UpdateOperation,
            cancellationToken);

        await _db.SaveChangesAsync(
            cancellationToken);

        return ToResult(supplier);
    }

    public async Task DeleteAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.GetRequiredTenantId();

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

        var now = DateTime.UtcNow;

        supplier.IsDeleted = true;
        supplier.IsActive = false;
        supplier.DeletedAtUtc = now;
        supplier.ModifiedAtUtc = now;

        if (!supplier.ServerId.HasValue || supplier.ServerId.Value == Guid.Empty)
        {
            var queueItems =
                await _db.SyncQueueItems
                    .Where(item =>
                        item.TenantId == tenantId &&
                        item.EntityName == SupplierEntityName &&
                        item.LocalEntityId == supplier.Id &&
                        item.Status != SyncQueueStatus.Done)
                    .ToListAsync(cancellationToken);

            _db.SyncQueueItems.RemoveRange(queueItems);

            supplier.SyncStatus = SyncQueueStatus.Done;
        }
        else
        {
            supplier.SyncStatus = SyncQueueStatus.Pending;

            await AddOrMergeQueueItemAsync(
                tenantId, supplier, DeleteOperation, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        // <-- stop here. Remove everything after this line.
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

                    supplier.Email != null &&
                    supplier.Email
                        .ToLower()
                        .Contains(search) ||

                    supplier.Phone != null &&
                    supplier.Phone
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
        LocalSupplier supplier,
        string operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        var pendingItems =
            await _db.SyncQueueItems
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.EntityName == SupplierEntityName &&
                    item.LocalEntityId == supplier.Id &&
                    item.Status != SyncQueueStatus.Done)
                .OrderBy(item =>
                    item.CreatedAtUtc)
                .ToListAsync(cancellationToken);

        var pendingCreate =
            pendingItems.FirstOrDefault(item =>
                item.Operation == CreateOperation);

        var pendingUpdate =
            pendingItems.FirstOrDefault(item =>
                item.Operation == UpdateOperation);

        var pendingDelete =
            pendingItems.FirstOrDefault(item =>
                item.Operation == DeleteOperation);

        if (operation == CreateOperation)
        {
            var payloadJson =
                CreatePayloadJson(
                    supplier,
                    CreateOperation);

            if (pendingCreate != null)
            {
                RefreshQueueItem(
                    pendingCreate,
                    supplier,
                    payloadJson);

                return;
            }

            _db.SyncQueueItems.Add(
                CreateQueueItem(
                    tenantId,
                    supplier,
                    CreateOperation,
                    payloadJson));

            return;
        }

        if (operation == UpdateOperation)
        {
            /*
             * Le client n'existe pas encore sur le serveur.
             * Le Create conserve son ClientOperationId, mais son
             * payload est remplacé par la version actuelle.
             */
            if (pendingCreate != null)
            {
                var createPayloadJson =
                    CreatePayloadJson(
                        supplier,
                        CreateOperation);

                RefreshQueueItem(
                    pendingCreate,
                    supplier,
                    createPayloadJson);

                return;
            }

            if (pendingDelete != null)
            {
                return;
            }

            var updatePayloadJson =
                CreatePayloadJson(
                    supplier,
                    UpdateOperation);

            if (pendingUpdate != null)
            {
                RefreshQueueItem(
                    pendingUpdate,
                    supplier,
                    updatePayloadJson);

                return;
            }

            _db.SyncQueueItems.Add(
                CreateQueueItem(
                    tenantId,
                    supplier,
                    UpdateOperation,
                    updatePayloadJson));

            return;
        }

        if (operation == DeleteOperation)
        {
            var deletePayloadJson =
                CreatePayloadJson(
                    supplier,
                    DeleteOperation);

            if (pendingDelete != null)
            {
                RefreshQueueItem(
                    pendingDelete,
                    supplier,
                    deletePayloadJson);

                return;
            }

            /*
             * Les Update deviennent inutiles lorsqu'un Delete
             * est ajouté.
             */
            var obsoleteUpdates =
                pendingItems.Where(item =>
                    item.Operation == UpdateOperation);

            _db.SyncQueueItems.RemoveRange(
                obsoleteUpdates);

            _db.SyncQueueItems.Add(
                CreateQueueItem(
                    tenantId,
                    supplier,
                    DeleteOperation,
                    deletePayloadJson));

            return;
        }

        throw new InvalidOperationException(
            $"Unsupported supplier sync operation '{operation}'.");
    }

    private static SyncQueueItem CreateQueueItem(
        Guid tenantId,
        LocalSupplier supplier,
        string operation,
        string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(
        payloadJson))
        {
            throw new InvalidOperationException(
                "A Supplier queue item cannot contain an empty payload.");
        }

        return new SyncQueueItem
        {
            Id = Guid.NewGuid(),

            TenantId = tenantId,

            ClientOperationId = Guid.NewGuid(),

            LocalEntityId = supplier.Id,

            ServerEntityId = supplier.ServerId,

            EntityName = SupplierEntityName,

            Operation = operation,

            PayloadJson = payloadJson,

            Status = SyncQueueStatus.Pending,

            Attempts = 0,

            ErrorMessage = null,

            CreatedAtUtc = DateTime.UtcNow,

            NextAttemptAtUtc = null,

            BatchId = null,

            LockedAtUtc = null
        };
    }

    private static void RefreshQueueItem(
    SyncQueueItem queueItem,
    LocalSupplier supplier,
    string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(
        payloadJson))
        {
            throw new InvalidOperationException(
                "A Supplier queue item cannot contain an empty payload.");
        }


        if (queueItem.Status ==
            SyncQueueStatus.Conflict)
        {
            queueItem.ClientOperationId =
                Guid.NewGuid();
        }

        queueItem.ServerEntityId = supplier.ServerId;

        queueItem.PayloadJson = payloadJson;

        queueItem.Status = SyncQueueStatus.Pending;

        queueItem.Attempts = 0;

        queueItem.ErrorMessage = null;

        queueItem.LastAttemptAtUtc = null;

        queueItem.ProcessedAtUtc = null;

        queueItem.NextAttemptAtUtc = null;

        queueItem.BatchId = null;

        queueItem.LockedAtUtc = null;
    }

    private static string CreatePayloadJson(
        LocalSupplier supplier,
        string operation)
    {
        if (operation == CreateOperation)
        {
            var request =
                new CreateSupplierRequest
                {
                    Name = supplier.Name,

                    Email = supplier.Email,

                    Phone = supplier.Phone,

                    Address = supplier.Address,

                    TaxNumber = supplier.TaxNumber,

                    IsActive = supplier.IsActive,
                    ContactPerson = supplier.ContactPerson,

                    City = supplier.City,

                    PostalCode = supplier.PostalCode,

                    Country = supplier.Country,

                    PaymentTermsDays = supplier.PaymentTermsDays,

                    BankAccount = supplier.BankAccount,

                    Notes = supplier.Notes
                };

            return JsonSerializer.Serialize(
                request);
        }

        if (operation == UpdateOperation)
        {
            var request =
                new UpdateSupplierRequest
                {
                    Id =
                        supplier.ServerId ??
                        Guid.Empty,

                    Name =
                        supplier.Name,

                    ContactPerson =
                        supplier.ContactPerson,

                    Email =
                        supplier.Email,

                    Phone =
                        supplier.Phone,

                    Address =
                        supplier.Address,

                    City =
                        supplier.City,

                    PostalCode =
                        supplier.PostalCode,

                    Country =
                        supplier.Country,

                    TaxNumber =
                        supplier.TaxNumber,

                    PaymentTermsDays =
                        supplier.PaymentTermsDays,

                    BankAccount =
                        supplier.BankAccount,

                    IsActive =
                        supplier.IsActive,

                    Notes =
                        supplier.Notes
                };

            return JsonSerializer.Serialize(
                request);
        }

        if (operation == DeleteOperation)
        {
            return JsonSerializer.Serialize(
                new
                {
                    ServerEntityId =
                        supplier.ServerId
                });
        }

        throw new InvalidOperationException(
            $"Unsupported supplier sync operation '{operation}'.");
    }

    private static void ValidateName(
        string? name)
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
    }

    private static SupplierResult ToResult(
        LocalSupplier supplier)
    {
        /*
         * Id reste l'identifiant SQLite.
         * L'interface utilise cet Id pour modifier/supprimer
         * la ligne locale.
         */
        return new SupplierResult
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            TaxNumber = supplier.TaxNumber,
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