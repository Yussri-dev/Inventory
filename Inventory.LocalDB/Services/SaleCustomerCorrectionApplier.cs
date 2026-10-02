using Inventory.Dto.Sales.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services;

/// <summary>Apply one authoritative server snapshot atomically; never requeue a downloaded correction.</summary>
public sealed class SaleCustomerCorrectionApplier(PosLocalDbContext db)
{
    public async Task<bool> ApplyAsync(Guid tenantId, SaleCustomerSyncResult snapshot, CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant required", nameof(tenantId));
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await HasPendingAsync(tenantId, snapshot, ct)) return false;
        var availableCustomers = snapshot.Customers.Select(x => x.CustomerId).ToHashSet();
        if (snapshot.Sales.Any(x => x.CustomerId.HasValue && !availableCustomers.Contains(x.CustomerId.Value)) ||
            snapshot.Entries.Any(x => !availableCustomers.Contains(x.CustomerId))) return false;
        var customerIds = snapshot.Customers.Select(x => x.CustomerId).ToArray();
        var customers = await db.Customers.Where(x => x.TenantId == tenantId && x.ServerId.HasValue && customerIds.Contains(x.ServerId.Value)).ToListAsync(ct);
        var byCustomer = customers.ToDictionary(x => x.ServerId!.Value);
        foreach (var item in snapshot.Customers.Where(x => !byCustomer.ContainsKey(x.CustomerId)))
        {
            var customer = new LocalCustomer { TenantId = tenantId, ServerId = item.CustomerId,
                Name = item.Name, Phone = item.Phone, Email = item.Email, Address = item.Address,
                TaxNumber = item.TaxNumber, Notes = item.Notes, IsActive = item.IsActive, IsDeleted = item.IsDeleted,
                AllowCredit = item.AllowCredit, HasUnlimitedCredit = item.HasUnlimitedCredit,
                CreditLimit = item.CreditLimit, SyncStatus = SyncQueueStatus.Done, LastSyncedAtUtc = DateTime.UtcNow };
            db.Customers.Add(customer);
            byCustomer[item.CustomerId] = customer;
        }
        var saleIds = snapshot.Sales.Select(x => x.SaleId).ToArray();
        var sales = await db.Sales.Where(x => x.TenantId == tenantId && x.ServerId.HasValue && saleIds.Contains(x.ServerId.Value)).ToListAsync(ct);
        var bySale = sales.ToDictionary(x => x.ServerId!.Value);
        foreach (var item in snapshot.Sales)
        {
            if (!bySale.TryGetValue(item.SaleId, out var sale) || sale.SyncStatus != SyncQueueStatus.Done) continue;
            sale.CustomerServerId = item.CustomerId;
            sale.CustomerLocalId = item.CustomerId.HasValue ? byCustomer[item.CustomerId.Value].Id : null;
            sale.LastSyncedAtUtc = DateTime.UtcNow;
        }
        foreach (var item in snapshot.Customers)
            if (byCustomer.TryGetValue(item.CustomerId, out var customer)) customer.CurrentBalance = item.Balance;
        var entryIds = snapshot.Entries.Select(x => x.Id).ToArray();
        var existingIds = await db.CustomerTransactions.Where(x => x.TenantId == tenantId && x.ServerId.HasValue && entryIds.Contains(x.ServerId.Value))
            .Select(x => x.ServerId!.Value).ToListAsync(ct);
        foreach (var item in snapshot.Entries.Where(x => !existingIds.Contains(x.Id)))
        {
            db.CustomerTransactions.Add(new LocalCustomerTransaction { TenantId = tenantId, ServerId = item.Id,
                ClientOperationId = item.OperationId, CustomerLocalId = byCustomer[item.CustomerId].Id,
                CustomerServerId = item.CustomerId, SaleServerId = item.SaleId,
                SaleLocalId = bySale.TryGetValue(item.SaleId, out var sale) ? sale.Id : null,
                Type = item.Type, Origin = "ServerCorrection", UploadRequired = false, IsCash = false,
                Amount = item.Amount, BalanceBefore = item.BalanceBefore, BalanceAfter = item.BalanceAfter,
                Description = item.Description, TransactionDateUtc = item.Date, CreatedAtUtc = item.Date,
                SyncStatus = SyncQueueStatus.Done, LastSyncedAtUtc = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private async Task<bool> HasPendingAsync(Guid tenantId, SaleCustomerSyncResult snapshot, CancellationToken ct)
    {
        // Protect only the sale and accounts touched by this snapshot. Unrelated outbox
        // conflicts must not prevent an already uploaded sale from receiving a correction.
        var serverSales = snapshot.Sales.Select(x => x.SaleId).ToArray();
        var sales = await db.Sales.Where(x => x.TenantId == tenantId && x.ServerId.HasValue && serverSales.Contains(x.ServerId.Value)).ToListAsync(ct);
        if (sales.Count != serverSales.Distinct().Count() || sales.Any(x => x.SyncStatus != SyncQueueStatus.Done)) return true;
        var localSales = sales.Select(x => x.Id).ToArray();
        var oldLocalCustomers = sales.Where(x => x.CustomerLocalId.HasValue).Select(x => x.CustomerLocalId!.Value).ToArray();
        var serverCustomers = snapshot.Customers.Select(x => x.CustomerId)
            .Concat(sales.Where(x => x.CustomerServerId.HasValue).Select(x => x.CustomerServerId!.Value)).Distinct().ToArray();
        var customers = await db.Customers.Where(x => x.TenantId == tenantId &&
            (oldLocalCustomers.Contains(x.Id) || (x.ServerId.HasValue && serverCustomers.Contains(x.ServerId.Value)))).ToListAsync(ct);
        if (customers.Any(x => x.SyncStatus != SyncQueueStatus.Done)) return true;
        var localCustomers = customers.Select(x => x.Id).ToArray();
        var queue = await db.SyncQueueItems.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status != SyncQueueStatus.Done &&
            (x.EntityName == "Sale" || x.EntityName == "Customer" || x.EntityName == "CustomerTransaction" || x.EntityName == "Return"))
            .Select(x => new { x.EntityName, x.LocalEntityId, x.ServerEntityId }).ToListAsync(ct);
        if (queue.Any(x => (x.EntityName == "Sale" && (localSales.Contains(x.LocalEntityId) ||
                (x.ServerEntityId.HasValue && serverSales.Contains(x.ServerEntityId.Value)))) ||
            (x.EntityName == "Customer" && (localCustomers.Contains(x.LocalEntityId) ||
                (x.ServerEntityId.HasValue && serverCustomers.Contains(x.ServerEntityId.Value)))))) return true;
        var queuedSales = queue.Where(x => x.EntityName == "Sale").Select(x => x.LocalEntityId).ToArray();
        if (await db.Sales.AnyAsync(x => x.TenantId == tenantId &&
            (x.SyncStatus != SyncQueueStatus.Done || queuedSales.Contains(x.Id)) &&
            ((x.CustomerLocalId.HasValue && localCustomers.Contains(x.CustomerLocalId.Value)) ||
             (x.CustomerServerId.HasValue && serverCustomers.Contains(x.CustomerServerId.Value))), ct)) return true;
        var queuedTransactions = queue.Where(x => x.EntityName == "CustomerTransaction").Select(x => x.LocalEntityId).ToArray();
        if (await db.CustomerTransactions.AnyAsync(x => x.TenantId == tenantId &&
            (x.SyncStatus != SyncQueueStatus.Done || queuedTransactions.Contains(x.Id)) &&
            (localCustomers.Contains(x.CustomerLocalId) || (x.CustomerServerId.HasValue && serverCustomers.Contains(x.CustomerServerId.Value)) ||
             (x.SaleLocalId.HasValue && localSales.Contains(x.SaleLocalId.Value)) || (x.SaleServerId.HasValue && serverSales.Contains(x.SaleServerId.Value))), ct)) return true;
        var queuedReturns = queue.Where(x => x.EntityName == "Return").Select(x => x.LocalEntityId).ToArray();
        return await db.Returns.AnyAsync(x => x.TenantId == tenantId &&
            (x.SyncStatus != SyncQueueStatus.Done || queuedReturns.Contains(x.Id)) &&
            (localSales.Contains(x.LocalSaleId) || (x.ServerSaleId.HasValue && serverSales.Contains(x.ServerSaleId.Value)) ||
             (x.CustomerLocalId.HasValue && localCustomers.Contains(x.CustomerLocalId.Value)) ||
             (x.CustomerServerId.HasValue && serverCustomers.Contains(x.CustomerServerId.Value))), ct);
    }
}
