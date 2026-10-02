using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace Inventory.Infrastructure.Data;

public interface ITenantDataAccess
{
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
    Guid TenantId { get; }
}

public sealed class TenantIsolationException : Exception
{
    public TenantIsolationException() : base("The operation is not permitted for the current company.") { }
}

public partial class InventoryDbContext
{
    private readonly Guid _requestTenantId;
    private readonly bool _authenticated;
    private readonly bool _superAdmin;
    private Guid? _provisioningTenantId;

    public Guid CurrentDataTenantId => _provisioningTenantId ?? _requestTenantId;
    public bool CanReadAllTenants => _superAdmin && !_provisioningTenantId.HasValue;

    private static bool IsTenantOwned(IReadOnlyEntityType type) =>
        type.FindProperty("TenantId")?.ClrType == typeof(Guid);

    private void ConfigureTenantFilters(ModelBuilder builder)
    {
        // Nullable Identity tenant membership remains available to the login flow.
        // Global catalogs/categories and Tenant administration use their own authorization.
        foreach (var type in builder.Model.GetEntityTypes().Where(t => t.BaseType == null && IsTenantOwned(t)).ToArray())
        {
            var entity = Expression.Parameter(type.ClrType, "entity");
            var tenant = Expression.Call(typeof(EF), nameof(EF.Property), new[] { typeof(Guid) },
                entity, Expression.Constant("TenantId"));
            var context = Expression.Constant(this);
            var current = Expression.Property(context, nameof(CurrentDataTenantId));
            var allowed = Expression.OrElse(
                Expression.Property(context, nameof(CanReadAllTenants)),
                Expression.AndAlso(Expression.NotEqual(current, Expression.Constant(Guid.Empty)),
                    Expression.Equal(tenant, current)));
            builder.Entity(type.ClrType).HasQueryFilter(Expression.Lambda(allowed, entity));
        }
    }

    // Called only by the server's catalog provisioning service after validating
    // the authenticated/registered user. Never bind this scope to an HTTP tenant parameter.
    public IDisposable BeginProductProvisioning(Guid tenantId)
    {
        if (tenantId == Guid.Empty ||
            (_authenticated && !_superAdmin && _requestTenantId != tenantId))
            throw new TenantIsolationException();
        var previous = _provisioningTenantId;
        _provisioningTenantId = tenantId;
        return new ProvisioningScope(this, previous);
    }

    private sealed class ProvisioningScope(InventoryDbContext context, Guid? previous) : IDisposable
    {
        public void Dispose() => context._provisioningTenantId = previous;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantWritesAsync(false, CancellationToken.None).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        await ValidateTenantWritesAsync(true, cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task ValidateTenantWritesAsync(bool asynchronous, CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        var entries = ChangeTracker.Entries()
            .Where(e => IsTenantOwned(e.Metadata) &&
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();

        foreach (var entry in entries)
        {
            var tenantId = (Guid)entry.Property("TenantId").CurrentValue!;
            if (tenantId == Guid.Empty || (!CanReadAllTenants && tenantId != CurrentDataTenantId))
                throw new TenantIsolationException();

            if (entry.State != EntityState.Added)
            {
                // Read the persisted owner, not the potentially forged owner of an attached DTO.
                var stored = asynchronous
                    ? await entry.GetDatabaseValuesAsync(cancellationToken)
                    : entry.GetDatabaseValues();
                if (stored == null || (Guid)stored["TenantId"]! != tenantId)
                    throw new TenantIsolationException();
            }

            if (entry.State == EntityState.Deleted) continue;

            foreach (var foreignKey in entry.Metadata.GetForeignKeys())
            {
                var principalType = foreignKey.PrincipalEntityType;
                var ownerProperty = principalType.FindProperty("TenantId");
                var isTenant = principalType.ClrType == typeof(Tenant);
                if (!isTenant && ownerProperty == null) continue;

                var values = foreignKey.Properties.Select(p => entry.Property(p.Name).CurrentValue).ToArray();
                if (values.Any(v => v == null)) continue;
                if (isTenant)
                {
                    if (values.Length != 1 || !Equals(values[0], tenantId))
                        throw new TenantIsolationException();
                    continue;
                }

                var principal = asynchronous
                    ? await FindAsync(principalType.ClrType, values, cancellationToken)
                    : Find(principalType.ClrType, values);
                if (principal == null) throw new TenantIsolationException();
                var principalEntry = Entry(principal);
                object? owner;
                if (principalEntry.State == EntityState.Added)
                    owner = principalEntry.Property("TenantId").CurrentValue;
                else
                {
                    var stored = asynchronous
                        ? await principalEntry.GetDatabaseValuesAsync(cancellationToken)
                        : principalEntry.GetDatabaseValues();
                    if (stored == null) throw new TenantIsolationException();
                    owner = stored["TenantId"];
                }
                // A global Identity account (SuperAdmin) can be an audit actor.
                if (owner is Guid principalTenant && principalTenant != tenantId)
                    throw new TenantIsolationException();
            }
        }
    }
}
