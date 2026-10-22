using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Results;
using Inventory.LocalDB.Services.Sync;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Microsoft.Extensions.Logging;
using Moq;

namespace dotNetApiProBoilerplate.Local.Tests
{
    public sealed class LocalBulkSyncUploaderTests
    {
        [Fact]
        public async Task SyncPendingAsync_ShouldReusePreparedPayload_OnRetry()
        {
            // Arrange
            var queueItem = new SyncQueueItem
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                EntityName = "Sale",
                LocalEntityId = Guid.NewGuid(),
                ClientOperationId = Guid.NewGuid(),
                Operation = SyncOperation.Create,
                PayloadJson = """{"original":true}""",
                PreparedPayloadJson = null,
                Status = SyncQueueStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            var preparedPayload =
                """{"prepared":true}""";

            var syncQueueServiceMock =
                new Mock<ISyncQueueService>();

            var syncBatchApiMock =
                new Mock<ISyncBatchApi>();

            var payloadBuilderMock =
                new Mock<ILocalSyncPayloadBuilder>();

            var loggerMock =
                new Mock<ILogger<LocalBulkSyncUploader>>();

            payloadBuilderMock
                .SetupGet(builder => builder.EntityName)
                .Returns("Sale");

            payloadBuilderMock
                .Setup(builder =>
                    builder.BuildPayloadJsonAsync(
                        It.IsAny<SyncQueueItem>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(preparedPayload);

            syncQueueServiceMock
                .Setup(service =>
                    service.SavePreparedPayloadAsync(
                        queueItem.Id,
                        preparedPayload,
                        It.IsAny<CancellationToken>()))
                .Callback<Guid, string, CancellationToken>(
                    (_, payload, _) =>
                    {
                        queueItem.PreparedPayloadJson =
                            payload;
                    })
                .Returns(Task.CompletedTask);

            syncQueueServiceMock
                .Setup(service =>
                    service.ClaimPendingBatchAsync(
                        It.IsAny<IReadOnlyCollection<string>>(),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                    new ClaimedSyncBatchResult
                    {
                        BatchId = Guid.NewGuid(),
                        TenantId = queueItem.TenantId,
                        ClaimedAtUtc = DateTime.UtcNow,

                        Items =
                        [
                            queueItem
                        ]
                    });

            syncBatchApiMock
                .Setup(api =>
                    api.UploadAsync(
                        It.IsAny<SyncBatchRequest>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    (SyncBatchRequest request,
                     CancellationToken _) =>
                        new SyncBatchResult
                        {
                            BatchId = request.BatchId,

                            Items =
                            [
                                new SyncBatchItemResult
                                {
                                    QueueItemId =
                                        queueItem.Id,

                                    ClientOperationId =
                                        queueItem.ClientOperationId,

                                    Status =
                                        SyncBatchItemStatus.Done
                                }
                            ]
                        });

            syncQueueServiceMock
                .Setup(service =>
                    service.ApplyBatchResultAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<SyncBatchResult>(),
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var uploader =
                new LocalBulkSyncUploader(
                    syncQueueServiceMock.Object,
                    syncBatchApiMock.Object,
                    new[]
                    {
                        payloadBuilderMock.Object
                    },
                    loggerMock.Object);

            // Act - first attempt
            await uploader.SyncPendingAsync(
                new[]
                {
                    "Sale"
                },
                maximumBatches: 1);

            // Act - retry
            await uploader.SyncPendingAsync(
                new[]
                {
                    "Sale"
                },
                maximumBatches: 1);

            // Assert
            payloadBuilderMock.Verify(
                builder =>
                    builder.BuildPayloadJsonAsync(
                        It.IsAny<SyncQueueItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            syncQueueServiceMock.Verify(
                service =>
                    service.SavePreparedPayloadAsync(
                        queueItem.Id,
                        preparedPayload,
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
