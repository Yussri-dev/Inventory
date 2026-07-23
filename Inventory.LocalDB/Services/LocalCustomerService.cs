using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services;

public sealed class LocalCustomerService
    : ILocalCustomerService
{
    private const string CustomerEntityName =
        "Customer";

    private const string CreateOperation =
        "Create";

    private const string UpdateOperation =
        "Update";

    private const string DeleteOperation =
        "Delete";

    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public LocalCustomerService(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<CustomerResult?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer id is required.",
                nameof(id));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var customer =
            await _db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == id &&
                        !item.IsDeleted,
                    cancellationToken);

        return customer == null
            ? null
            : ToResult(customer);
    }

    public async Task<CustomerResult> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        ValidateName(request.Name);

        var normalizedName =
            request.Name.Trim();

        var exists =
            await _db.Customers
                .AsNoTracking()
                .AnyAsync(
                    customer =>
                        customer.TenantId == tenantId &&
                        !customer.IsDeleted &&
                        customer.Name.ToLower() ==
                        normalizedName.ToLower(),
                    cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                $"Customer '{normalizedName}' already exists locally.");
        }

        var now =
            DateTime.UtcNow;

        var customer =
            new LocalCustomer
            {
                Id = Guid.NewGuid(),
                ServerId = null,
                TenantId = tenantId,

                Name = normalizedName,
                Email = NormalizeNullable(request.Email),
                Phone = NormalizeNullable(request.Phone),
                Address = NormalizeNullable(request.Address),
                TaxNumber = NormalizeNullable(request.TaxNumber),

                CreditLimit = request.CreditLimit,
                CurrentBalance = 0m,

                IsActive = request.IsActive,
                IsDeleted = false,

                Notes = NormalizeNullable(request.Notes),

                SyncStatus = SyncQueueStatus.Pending,
                CreatedAtUtc = now
            };

        _db.Customers.Add(customer);

        await AddOrMergeQueueItemAsync(
            tenantId,
            customer.Id,
            CreateOperation,
            cancellationToken);

        await _db.SaveChangesAsync(
            cancellationToken);

        return ToResult(customer);
    }

    public async Task<CustomerResult> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        ValidateName(request.Name);

        var customer =
            await _db.Customers
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == id &&
                        !item.IsDeleted,
                    cancellationToken);

        if (customer == null)
        {
            throw new InvalidOperationException(
                "Customer not found locally.");
        }

        var normalizedName =
            request.Name.Trim();

        var nameExists =
            await _db.Customers
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
                $"Customer '{normalizedName}' already exists locally.");
        }

        customer.Name =
            normalizedName;

        customer.Email =
            NormalizeNullable(request.Email);

        customer.Phone =
            NormalizeNullable(request.Phone);

        customer.Address =
            NormalizeNullable(request.Address);

        customer.TaxNumber =
            NormalizeNullable(request.TaxNumber);

        customer.CreditLimit =
            request.CreditLimit;

        customer.IsActive =
            request.IsActive;

        customer.Notes =
            NormalizeNullable(request.Notes);

        customer.ModifiedAtUtc =
            DateTime.UtcNow;

        customer.SyncStatus =
            SyncQueueStatus.Pending;

        await AddOrMergeQueueItemAsync(
            tenantId,
            customer.Id,
            UpdateOperation,
            cancellationToken);

        await _db.SaveChangesAsync(
            cancellationToken);

        return ToResult(customer);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var customer =
            await _db.Customers
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == id &&
                        !item.IsDeleted,
                    cancellationToken);

        if (customer == null)
        {
            throw new InvalidOperationException(
                "Customer not found locally.");
        }

        var now =
            DateTime.UtcNow;

        customer.IsDeleted =
            true;

        customer.IsActive =
            false;

        customer.DeletedAtUtc =
            now;

        customer.ModifiedAtUtc =
            now;

        if (!customer.ServerId.HasValue ||
            customer.ServerId.Value == Guid.Empty)
        {
            /*
             * Le client n'a jamais existé sur le serveur.
             * On supprime donc les opérations Create/Update.
             */
            var queueItems =
                await _db.SyncQueueItems
                    .Where(item =>
                        item.TenantId == tenantId &&
                        item.EntityName == CustomerEntityName &&
                        item.LocalEntityId == customer.Id &&
                        item.Status != SyncQueueStatus.Done)
                    .ToListAsync(cancellationToken);

            _db.SyncQueueItems.RemoveRange(queueItems);

            customer.SyncStatus =
                SyncQueueStatus.Done;
        }
        else
        {
            customer.SyncStatus =
                SyncQueueStatus.Pending;

            await AddOrMergeQueueItemAsync(
                tenantId,
                customer.Id,
                DeleteOperation,
                cancellationToken);
        }

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<PagedResult<CustomerResult>> QueryAsync(
        CustomerQuery query,
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

        var customers =
            _db.Customers
                .AsNoTracking()
                .Where(customer =>
                    customer.TenantId == tenantId &&
                    !customer.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search =
                query.Search
                    .Trim()
                    .ToLower();

            customers =
                customers.Where(customer =>
                    customer.Name
                        .ToLower()
                        .Contains(search) ||

                    customer.Email != null &&
                    customer.Email
                        .ToLower()
                        .Contains(search) ||

                    customer.Phone != null &&
                    customer.Phone
                        .ToLower()
                        .Contains(search));
        }

        customers =
            query.SortBy?
                .Trim()
                .ToLowerInvariant() switch
            {
                "name" =>
                    query.Desc
                        ? customers.OrderByDescending(
                            customer => customer.Name)
                        : customers.OrderBy(
                            customer => customer.Name),

                "currentbalance" =>
                    query.Desc
                        ? customers.OrderByDescending(
                            customer =>
                                customer.CurrentBalance)
                        : customers.OrderBy(
                            customer =>
                                customer.CurrentBalance),

                _ =>
                    query.Desc
                        ? customers.OrderByDescending(
                            customer =>
                                customer.CreatedAtUtc)
                        : customers.OrderBy(
                            customer =>
                                customer.CreatedAtUtc)
            };

        var total =
            await customers.CountAsync(
                cancellationToken);

        var items =
            await customers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        return new PagedResult<CustomerResult>
        {
            Items = items
                .Select(ToResult)
                .ToList(),

            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<CustomerResult>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var customers =
            await _db.Customers
                .AsNoTracking()
                .Where(customer =>
                    customer.TenantId == tenantId &&
                    !customer.IsDeleted &&
                    customer.IsActive)
                .OrderBy(customer =>
                    customer.Name)
                .ToListAsync(cancellationToken);

        return customers
            .Select(ToResult)
            .ToList();
    }

    private async Task AddOrMergeQueueItemAsync(
        Guid tenantId,
        Guid localCustomerId,
        string operation,
        CancellationToken cancellationToken)
    {
        var pendingItems =
            await _db.SyncQueueItems
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.EntityName == CustomerEntityName &&
                    item.LocalEntityId == localCustomerId &&
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
            if (pendingCreate != null)
                return;

            _db.SyncQueueItems.Add(
                CreateQueueItem(
                    tenantId,
                    localCustomerId,
                    CreateOperation));

            return;
        }

        if (operation == UpdateOperation)
        {
            /*
             * Le Create en attente lira la version actuelle
             * du LocalCustomer. Aucun Update supplémentaire
             * n'est nécessaire.
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
                    localCustomerId,
                    UpdateOperation));

            return;
        }

        if (operation == DeleteOperation)
        {
            if (pendingDelete != null)
                return;

            /*
             * Les Update deviennent inutiles lorsqu'un Delete
             * est ajouté.
             */
            var updates =
                pendingItems.Where(item =>
                    item.Operation == UpdateOperation);

            _db.SyncQueueItems.RemoveRange(
                updates);

            _db.SyncQueueItems.Add(
                CreateQueueItem(
                    tenantId,
                    localCustomerId,
                    DeleteOperation));

            return;
        }

        throw new InvalidOperationException(
            $"Unsupported customer sync operation '{operation}'.");
    }

    private static SyncQueueItem CreateQueueItem(
        Guid tenantId,
        Guid localCustomerId,
        string operation)
    {
        return new SyncQueueItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,

            ClientOperationId =
                Guid.NewGuid(),

            LocalEntityId =
                localCustomerId,

            EntityName =
                CustomerEntityName,

            Operation =
                operation,

            Status =
                SyncQueueStatus.Pending,

            Attempts = 0,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static void ValidateName(
        string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Customer name is required.");
        }

        if (name.Trim().Length > 200)
        {
            throw new InvalidOperationException(
                "Customer name cannot exceed 200 characters.");
        }
    }

    private static CustomerResult ToResult(
        LocalCustomer customer)
    {
        /*
         * Id reste l'identifiant SQLite.
         * L'interface utilise cet Id pour modifier/supprimer
         * la ligne locale.
         */
        return new CustomerResult
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            TaxNumber = customer.TaxNumber,
            CreditLimit = customer.CreditLimit,
            CurrentBalance = customer.CurrentBalance,
            IsActive = customer.IsActive,
            Notes = customer.Notes
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