using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Inventory.Dto.Sales.Results;

namespace Inventory.Ui.Services.Sync;

public sealed class SaleCustomerCorrectionSyncService(PosLocalDbContext db, ILocalTenantContext tenant,
    ISaleApi api, ILogger<SaleCustomerCorrectionSyncService> logger)
{
    public async Task<int> PullAsync(CancellationToken ct)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var applier = new SaleCustomerCorrectionApplier(db);
        var applied = 0;
        var deferred = 0;
        var ids = await db.Sales.AsNoTracking().Where(x => x.TenantId == tenantId &&
            x.ServerId.HasValue && x.SyncStatus == SyncQueueStatus.Done).OrderBy(x => x.Id)
            .Select(x => x.ServerId!.Value).ToListAsync(ct);
        foreach (var batch in ids.Chunk(250))
        {
            var snapshot = await api.GetCustomerSnapshots(batch, ct);
            foreach (var sale in snapshot.Sales)
            {
                var entries = snapshot.Entries.Where(x => x.SaleId == sale.SaleId).ToList();
                var customers = entries.Select(x => x.CustomerId).ToHashSet();
                if (sale.CustomerId.HasValue) customers.Add(sale.CustomerId.Value);
                var item = new SaleCustomerSyncResult { Sales = new() { sale }, Entries = entries,
                    Customers = snapshot.Customers.Where(x => customers.Contains(x.CustomerId)).ToList() };
                if (await applier.ApplyAsync(tenantId, item, ct)) applied++;
                else deferred++;
            }
        }
        logger.LogInformation("Server-to-SQLite sale/customer sync: applied {Applied}, deferred {Deferred} due to related pending local changes.", applied, deferred);
        return deferred;
    }
}

