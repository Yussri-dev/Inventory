using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Inventory.Ui.Services.Sync
{
    public sealed class SyncCoordinator
        : IAsyncDisposable
    {
        private const int BatchSize =
            250;

        private const int MaximumBatchesPerRun =
            20;

        /*
         * Normal changes are grouped until the application
         * has been quiet for two minutes.
         */
        private static readonly TimeSpan QuietPeriod =
            TimeSpan.FromMinutes(2);

        /*
         * Continuous activity cannot postpone synchronization
         * indefinitely.
         */
        private static readonly TimeSpan MaximumBatchWindow =
            TimeSpan.FromMinutes(10);

        /*
         * Reference entities required before uploading sales.
         */
        private static readonly string[] ReferenceEntityNames =
        {
            "Customer",
            "Supplier",
            "Product",
            "StockMovement",
            "Damage"
        };

        private static readonly string[] SaleEntityNames =
        {
            "Sale"
        };

        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ILogger<SyncCoordinator> _logger;

        private readonly Channel<bool> _signals =
            Channel.CreateBounded<bool>(
                new BoundedChannelOptions(1)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode =
                        BoundedChannelFullMode.DropWrite
                });

        private readonly CancellationTokenSource
            _shutdownSource =
                new();

        private readonly object _lifecycleLock =
            new();

        private Task? _runner;

        private bool _disposed;

        private int _forceRequested;

        private int _isSynchronizing;

        public SyncCoordinator(
            IServiceScopeFactory scopeFactory,
            ILogger<SyncCoordinator> logger)
        {
            ArgumentNullException.ThrowIfNull(
                scopeFactory);

            ArgumentNullException.ThrowIfNull(
                logger);

            _scopeFactory =
                scopeFactory;

            _logger =
                logger;
        }

        public bool IsSynchronizing =>
            Volatile.Read(
                ref _isSynchronizing) == 1;

        /*
         * Records a pending synchronization request.
         * No HTTP request is performed here.
         */
        public void RequestSync()
        {
            NotifyPendingWork();
        }

        public void NotifyPendingWork()
        {
            if (!EnsureStarted())
            {
                return;
            }

            _signals.Writer.TryWrite(
                true);
        }

        /*
         * Used by an explicit user action such as
         * a "Sync now" button.
         */
        public void SyncNow()
        {
            if (!EnsureStarted())
            {
                return;
            }

            Interlocked.Exchange(
                ref _forceRequested,
                1);

            _signals.Writer.TryWrite(
                true);
        }

        private bool EnsureStarted()
        {
            lock (_lifecycleLock)
            {
                if (_disposed)
                {
                    return false;
                }

                if (_runner != null)
                {
                    return true;
                }

                Connectivity.Current.ConnectivityChanged +=
                    OnConnectivityChanged;

                _runner =
                    RunAsync(
                        _shutdownSource.Token);

                return true;
            }
        }

        private async Task RunAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                await foreach (
                    var signal in
                    _signals.Reader.ReadAllAsync(
                        cancellationToken))
                {
                    _ = signal;

                    DrainSignals();

                    var forceRequested =
                        Interlocked.Exchange(
                            ref _forceRequested,
                            0) == 1;

                    if (!forceRequested)
                    {
                        await WaitForBatchWindowAsync(
                            cancellationToken);
                    }

                    if (Connectivity.Current.NetworkAccess !=
                        NetworkAccess.Internet)
                    {
                        continue;
                    }

                    Interlocked.Exchange(
                        ref _isSynchronizing,
                        1);

                    try
                    {
                        await ExecuteSynchronizationAsync(
                            cancellationToken);
                    }
                    catch (OperationCanceledException)
                        when (
                            cancellationToken
                                .IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(
                            exception,
                            "Background synchronization failed. " +
                            "Queued operations remain available " +
                            "for retry.");
                    }
                    finally
                    {
                        Interlocked.Exchange(
                            ref _isSynchronizing,
                            0);
                    }
                }
            }
            catch (OperationCanceledException)
                when (
                    cancellationToken
                        .IsCancellationRequested)
            {
            }
        }

        private async Task WaitForBatchWindowAsync(
            CancellationToken cancellationToken)
        {
            var windowStartedAtUtc =
                DateTime.UtcNow;

            while (true)
            {
                if (Interlocked.Exchange(
                        ref _forceRequested,
                        0) == 1)
                {
                    DrainSignals();

                    return;
                }

                var elapsed =
                    DateTime.UtcNow -
                    windowStartedAtUtc;

                var remainingMaximumDelay =
                    MaximumBatchWindow -
                    elapsed;

                if (remainingMaximumDelay <=
                    TimeSpan.Zero)
                {
                    return;
                }

                var delay =
                    remainingMaximumDelay <
                    QuietPeriod
                        ? remainingMaximumDelay
                        : QuietPeriod;

                using var waitSource =
                    CancellationTokenSource
                        .CreateLinkedTokenSource(
                            cancellationToken);

                var signalTask =
                    _signals.Reader
                        .WaitToReadAsync(
                            waitSource.Token)
                        .AsTask();

                var delayTask =
                    Task.Delay(
                        delay,
                        waitSource.Token);

                var completedTask =
                    await Task.WhenAny(
                        signalTask,
                        delayTask);

                if (completedTask ==
                    signalTask)
                {
                    var canRead =
                        await signalTask;

                    waitSource.Cancel();

                    await IgnoreCancellationAsync(
                        delayTask);

                    if (!canRead)
                    {
                        return;
                    }

                    DrainSignals();

                    continue;
                }

                await delayTask;

                waitSource.Cancel();

                await IgnoreCancellationAsync(
                    signalTask);

                return;
            }
        }

        private void DrainSignals()
        {
            while (_signals.Reader.TryRead(
                       out _))
            {
            }
        }

        private async Task ExecuteSynchronizationAsync(
    CancellationToken cancellationToken)
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            /*
             * Phase 1:
             * Cash-session creation and legacy prerequisites.
             */
            var prerequisiteUploader =
                scope.ServiceProvider
                    .GetRequiredService<ILocalSyncUploader>();

            var prerequisiteResult =
                await prerequisiteUploader.SyncPendingAsync(
                    cancellationToken);

            /*
             * Do not continue when the API is unavailable, the local
             * database is locked, or a prerequisite operation failed.
             */
            if (prerequisiteResult.Failed > 0 ||
                prerequisiteResult.Skipped > 0)
            {
                _logger.LogWarning(
                    "Synchronization stopped after the prerequisite phase. " +
                    "Failed={Failed}, Skipped={Skipped}, Messages={Messages}.",
                    prerequisiteResult.Failed,
                    prerequisiteResult.Skipped,
                    string.Join(
                        " | ",
                        prerequisiteResult.Messages));

                return;
            }

            /*
             * Phase 2:
             * Reference entities required by sales.
             */
            var bulkUploader =
                scope.ServiceProvider
                    .GetRequiredService<ILocalBulkSyncUploader>();

            var referenceResult =
                await bulkUploader.SyncPendingAsync(
                    ReferenceEntityNames,
                    batchSize:
                        BatchSize,
                    maximumBatches:
                        MaximumBatchesPerRun,
                    cancellationToken:
                        cancellationToken);

            if (referenceResult.Failed > 0 ||
                referenceResult.Conflicts > 0)
            {
                _logger.LogWarning(
                    "Synchronization stopped after the reference phase. " +
                    "Failed={Failed}, Conflicts={Conflicts}, " +
                    "Messages={Messages}.",
                    referenceResult.Failed,
                    referenceResult.Conflicts,
                    string.Join(
                        " | ",
                        referenceResult.Messages));

                return;
            }

            /*
             * Phase 3:
             * Sales must be synchronized before standalone customer
             * transactions and cash-session closure.
             */
            var saleResult =
                await bulkUploader.SyncPendingAsync(
                    SaleEntityNames,
                    batchSize:
                        BatchSize,
                    maximumBatches:
                        MaximumBatchesPerRun,
                    cancellationToken:
                        cancellationToken);

            if (saleResult.Failed > 0 ||
                saleResult.Conflicts > 0)
            {
                _logger.LogWarning(
                    "Synchronization stopped after the sale phase. " +
                    "Failed={Failed}, Conflicts={Conflicts}, " +
                    "Messages={Messages}.",
                    saleResult.Failed,
                    saleResult.Conflicts,
                    string.Join(
                        " | ",
                        saleResult.Messages));

                return;
            }

            /*
             * Phase 4:
             * Standalone customer payments and refunds.
             */
            var customerTransactionUploader =
                scope.ServiceProvider
                    .GetRequiredService<
                        ILocalCustomerTransactionUploadService>();

            var customerTransactionResult =
                await customerTransactionUploader
                    .UploadPendingAsync(
                        cancellationToken);

            if (customerTransactionResult.Failed > 0)
            {
                _logger.LogWarning(
                    "Synchronization stopped after the customer-transaction " +
                    "phase. Failed={Failed}, Skipped={Skipped}, " +
                    "Messages={Messages}.",
                    customerTransactionResult.Failed,
                    customerTransactionResult.Skipped,
                    string.Join(
                        " | ",
                        customerTransactionResult.Messages));

                return;
            }

            /*
             * Phase 5:
             * Pull authoritative stock.
             */
            var stockSyncService =
                scope.ServiceProvider
                    .GetRequiredService<ILocalStockSyncService>();

            await stockSyncService.FullSyncAsync(
                cancellationToken);

            /*
             * Phase 6:
             * Close sessions only after all dependent operations.
             */
            var closureResult =
                await prerequisiteUploader
                    .SyncCashSessionClosuresAsync(
                        cancellationToken);

            if (closureResult.Failed > 0 ||
                closureResult.Skipped > 0)
            {
                _logger.LogWarning(
                    "Cash-session closure was not fully completed. " +
                    "Failed={Failed}, Skipped={Skipped}, Messages={Messages}.",
                    closureResult.Failed,
                    closureResult.Skipped,
                    string.Join(
                        " | ",
                        closureResult.Messages));
            }
        }

        private void OnConnectivityChanged(
            object? sender,
            ConnectivityChangedEventArgs args)
        {
            if (args.NetworkAccess ==
                NetworkAccess.Internet)
            {
                NotifyPendingWork();
            }
        }

        private static async Task IgnoreCancellationAsync(
            Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async ValueTask DisposeAsync()
        {
            Task? runner;

            lock (_lifecycleLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed =
                    true;

                if (_runner != null)
                {
                    Connectivity.Current
                        .ConnectivityChanged -=
                            OnConnectivityChanged;
                }

                _shutdownSource.Cancel();

                _signals.Writer.TryComplete();

                runner =
                    _runner;
            }

            if (runner != null)
            {
                try
                {
                    await runner;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _shutdownSource.Dispose();
        }
    }
}