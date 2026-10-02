using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Sync;
using Inventory.LocalDB.Services.Sync.Handlers;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

int failures = 0;
foreach (var scenario in new[] {"offline-reconnect", "lost-response-duplicate", "cancel-retry", "crash-after-claim"}) {
 try { await Run(scenario); Console.WriteLine($"PASS {scenario}"); }
 catch(Exception ex) { failures++; Console.WriteLine($"FAIL {scenario}: {ex}"); }
}
return failures == 0 ? 0 : 1;

static void Check(bool ok, string message) { if(!ok) throw new Exception(message); }
static async Task Run(string mode) {
 await using var connection = new SqliteConnection("Data Source=:memory:");
 await connection.OpenAsync();
 var options = new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(connection).Options;
 await using var db = new PosLocalDbContext(options);
 await db.Database.EnsureCreatedAsync();
 var tenant = new TestTenant();
 var product = new LocalProduct { TenantId=tenant.GetRequiredTenantId(), ServerId=Guid.NewGuid(), Name="Scenario product", Barcode="SCENARIO", IsTracked=true, LocalStockQuantity=10 };
 db.Products.Add(product);
 db.Stocks.Add(new LocalStock { TenantId=tenant.GetRequiredTenantId(), ProductLocalId=product.Id, ProductServerId=product.ServerId, Quantity=10 });
 db.CashSessions.Add(new LocalCashSession { TenantId=tenant.GetRequiredTenantId(), ServerId=Guid.NewGuid(), SessionNumber="TEST" });
 await db.SaveChangesAsync();
 var sale = new LocalSale {
  Lines = new List<LocalSaleLine> {new() {ProductLocalId=product.Id, ProductServerId=product.ServerId, UnitProductLocalId=product.Id, UnitProductServerId=product.ServerId.Value, ProductName=product.Name, Quantity=2, UnitQuantity=2, UnitPrice=10}},
  Payments = new List<LocalPayment> {new() {Method=Inventory.Dto.Enums.PaymentMethod.Cash, Amount=20}}
 };
 await new LocalSaleService(db,tenant).CreateAsync(sale);
 db.ChangeTracker.Clear();
 Check(await db.Sales.CountAsync()==1, "Sale not persisted");
 Check((await db.Stocks.SingleAsync()).Quantity==8, "Offline stock must be 8");
 Check(await db.SyncQueueItems.CountAsync()==1, "Expected one queued sale");
 var queue = new SyncQueueService(db,tenant,new[] {new SaleLocalSyncResultHandler(db,tenant)});
 var api = new SimulatedApi {Mode=mode};
 var uploader = new LocalBulkSyncUploader(queue,api,new[] {new SaleLocalSyncPayloadBuilder(db,tenant)},NullLogger<LocalBulkSyncUploader>.Instance);
 if(mode=="crash-after-claim") {
  var batch=await queue.ClaimPendingBatchAsync(new[]{"Sale"},250);
  Check(batch.Items.Count==1,"Claim failed");
  db.ChangeTracker.Clear();
  await using var restartedDb=new PosLocalDbContext(options);
  var restartedQueue=new SyncQueueService(restartedDb,tenant,new[] {new SaleLocalSyncResultHandler(restartedDb,tenant)});
  var recovered=await restartedQueue.ClaimPendingBatchAsync(new[]{"Sale"},250);
  Check(recovered.Items.Count==0,"Unexpected automatic bulk recovery");
  // Execute the actual legacy recovery method from the compiled MAUI assembly.
  var uiPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../Inventory.Ui/bin/Debug/net9.0-windows10.0.19041.0/win10-x64/Inventory.Ui.dll"));
  System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (context,name) => {
   var path=Path.Combine(Path.GetDirectoryName(uiPath)!,name.Name+".dll");
   return File.Exists(path)?context.LoadFromAssemblyPath(path):null;
  };
  var assembly=System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(uiPath);
  var type=assembly.GetType("Inventory.Ui.Services.Sync.LocalSyncUploader",true)!;
  var instance=System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
  var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  type.GetField("_db",flags)!.SetValue(instance,restartedDb);
  type.GetField("_tenantContext",flags)!.SetValue(instance,tenant);
  var loggerType=typeof(NullLogger<>).MakeGenericType(type);
  type.GetField("_logger",flags)!.SetValue(instance,Activator.CreateInstance(loggerType));
  await (Task)type.GetMethod("RecoverInterruptedQueueItemsAsync",flags)!.Invoke(instance,new object[]{tenant.GetRequiredTenantId(),CancellationToken.None})!;
  recovered=await restartedQueue.ClaimPendingBatchAsync(new[]{"Sale"},250);
  Check(recovered.Items.Count==1,"Legacy recovery did not unlock the sale");
  Check(recovered.Items[0].ClientOperationId==batch.Items[0].ClientOperationId,"Recovery changed operation identity");
  await restartedQueue.ReleaseBatchAsync(recovered.BatchId,"Test retry");
  restartedDb.ChangeTracker.Clear();
  var retryItem=await restartedDb.SyncQueueItems.SingleAsync();
  retryItem.NextAttemptAtUtc=DateTime.UtcNow.AddSeconds(-1);
  await restartedDb.SaveChangesAsync();
  var restartedUploader=new LocalBulkSyncUploader(restartedQueue,api,new[]{new SaleLocalSyncPayloadBuilder(restartedDb,tenant)},NullLogger<LocalBulkSyncUploader>.Instance);
  api.Mode="online";
  var resumed=await restartedUploader.SyncPendingAsync(new[]{"Sale"});
  Check(resumed.Done==1,"Recovered sale did not synchronize");
  Check((await restartedDb.Stocks.SingleAsync()).Quantity==8,"Recovery changed stock twice");
  return;
 }
 using var cancel = new CancellationTokenSource();
 api.Cancel=cancel;
 var first=await uploader.SyncPendingAsync(new[]{"Sale"},cancellationToken:cancel.Token);
 db.ChangeTracker.Clear();
 var item=await db.SyncQueueItems.SingleAsync();
 Check(item.Status==SyncQueueStatus.Failed,"Interrupted upload should be Failed: "+string.Join(" | ",first.Messages));
 Check(item.BatchId==null && item.LockedAtUtc==null,"Claim not released");
 Check(item.NextAttemptAtUtc.HasValue,"Retry delay missing");
 Check(first.WasCancelled == (mode=="cancel-retry"),"Cancellation result incorrect");
 // Advance the retry deadline without a wall-clock wait.
 item.NextAttemptAtUtc=DateTime.UtcNow.AddSeconds(-1);
 await db.SaveChangesAsync();
 api.Mode="online";
 var second=await uploader.SyncPendingAsync(new[]{"Sale"});
 db.ChangeTracker.Clear();
 item=await db.SyncQueueItems.SingleAsync();
 Check(item.Status==SyncQueueStatus.Done,"Retry failed: "+string.Join(" | ",second.Messages));
 Check(api.Accepted.Count==1,"Expected one unique operation at simulated server");
 Check(api.OperationIds.Distinct().Count()==1,"ClientOperationId changed on retry");
 Check((await db.Sales.SingleAsync()).ServerId==api.Accepted.Values.Single(),"Server identity not reconciled");
 Check((await db.Payments.SingleAsync()).SyncStatus==SyncQueueStatus.Done,"Payment not reconciled");
 Check((await db.Stocks.SingleAsync()).Quantity==8,"Retry deducted stock again");
 Check(await db.Sales.CountAsync()==1 && await db.Payments.CountAsync()==1,"Local duplicate");
 if(mode=="lost-response-duplicate") Check(second.Duplicates==1,"Duplicate response not accepted");
 var third=await uploader.SyncPendingAsync(new[]{"Sale"});
 Check(third.Claimed==0,"Completed sale uploaded again");
}

sealed class TestTenant : ILocalTenantContext {
 public Guid? TenantId {get; private set;}=Guid.NewGuid();
 public bool HasTenant=>TenantId.HasValue;
 public void SetTenant(Guid id)=>TenantId=id;
 public Guid GetRequiredTenantId()=>TenantId!.Value;
 public void Clear()=>TenantId=null;
}
sealed class SimulatedApi : ISyncBatchApi {
 public string Mode="online";
 public CancellationTokenSource? Cancel;
 public Dictionary<Guid,Guid> Accepted=new();
 public List<Guid> OperationIds=new();
 public Task<SyncBatchResult> UploadAsync(SyncBatchRequest request,CancellationToken cancellationToken=default) {
  foreach(var op in request.Operations) OperationIds.Add(op.ClientOperationId);
  if(Mode=="offline-reconnect") throw new HttpRequestException("Simulated offline");
  if(Mode=="cancel-retry") {Cancel!.Cancel(); throw new OperationCanceledException(cancellationToken);}
  var result=new SyncBatchResult{BatchId=request.BatchId};
  foreach(var op in request.Operations) {
   bool duplicate=Accepted.TryGetValue(op.ClientOperationId,out var id);
   if(!duplicate){id=Guid.NewGuid();Accepted.Add(op.ClientOperationId,id);}
   result.Items.Add(new SyncBatchItemResult{QueueItemId=op.QueueItemId,ClientOperationId=op.ClientOperationId,ServerEntityId=id,ServerReferenceNumber="TEST-001",Status=duplicate?SyncBatchItemStatus.Duplicate:SyncBatchItemStatus.Done});
  }
  if(Mode=="lost-response-duplicate") throw new HttpRequestException("Simulated lost response after server commit");
  return Task.FromResult(result);
 }
}