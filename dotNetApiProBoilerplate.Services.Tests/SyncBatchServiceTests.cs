using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Inventory.Services.Abstractions;
using Inventory.Services.Sync;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Inventory.Services.Tests
{
    public sealed class SyncBatchServiceTests
    {
        [Fact]
        public async Task ProcessAsync_WhenOneBatchOperationThrows_ShouldIsolateFailure()
        {
            // Arrange
            var legacyHandlers =
                Array.Empty<ISyncOperationHandler>();

            var batchHandlerMock =
                new Mock<ISyncBatchOperationHandler>();

            batchHandlerMock
                .SetupGet(x => x.EntityName)
                .Returns("Product");

            var batchExecutorMock =
                new Mock<ISyncBatchOperationExecutor>();

            var operationExecutorMock =
                new Mock<ISyncOperationExecutor>();

            var loggerMock =
                new Mock<ILogger<SyncBatchService>>();

            var operationA =
                CreateOperation();

            var operationB =
                CreateOperation();

            var operationC =
                CreateOperation();

            var calls =
                new List<IReadOnlyList<SyncBatchOperationRequest>>();

            batchExecutorMock
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyList<SyncBatchOperationRequest>>(),
                    It.IsAny<ISyncBatchOperationHandler>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Guid,
                         IReadOnlyList<SyncBatchOperationRequest>,
                         ISyncBatchOperationHandler,
                         CancellationToken>(
                    (batchId, operations, handler, cancellationToken) =>
                    {
                        calls.Add(
                            operations.ToArray());

                        var containsOperationB =
                            operations.Any(x =>
                                x.QueueItemId ==
                                operationB.QueueItemId);

                        if (operations.Count > 1 &&
                            containsOperationB)
                        {
                            throw new InvalidOperationException(
                                "Forced test failure");
                        }

                        var results =
                            operations
                                .Select(operation =>
                                    new SyncBatchItemResult
                                    {
                                        QueueItemId =
                                            operation.QueueItemId,

                                        ClientOperationId =
                                            operation.ClientOperationId,

                                        ServerEntityId =
                                            operation.QueueItemId ==
                                            operationB.QueueItemId
                                                ? operation.ServerEntityId
                                                : Guid.NewGuid(),

                                        Status =
                                            operation.QueueItemId ==
                                            operationB.QueueItemId
                                                ? SyncBatchItemStatus.Failed
                                                : SyncBatchItemStatus.Done,

                                        ErrorMessage =
                                            operation.QueueItemId ==
                                            operationB.QueueItemId
                                                ? "Forced isolated failure"
                                                : null
                                    })
                                .ToArray();

                        return Task.FromResult<
                            IReadOnlyList<SyncBatchItemResult>>(
                                results);
                    });

            var service =
                new SyncBatchService(
                    legacyHandlers,
                    new[]
                    {
                        batchHandlerMock.Object
                    },
                    operationExecutorMock.Object,
                    batchExecutorMock.Object,
                    loggerMock.Object);

            var request =
                new SyncBatchRequest
                {
                    BatchId =
                        Guid.NewGuid(),

                    Operations =
                    [
                        operationA,
                        operationB,
                        operationC
                    ]
                };

            // Act
            var result =
                await service.ProcessAsync(
                    request);

            // Assert
            Assert.Equal(
                3,
                result.Items.Count);

            var resultA =
                Assert.Single(
                    result.Items.Where(x =>
                        x.QueueItemId ==
                        operationA.QueueItemId));

            var resultB =
                Assert.Single(
                    result.Items.Where(x =>
                        x.QueueItemId ==
                        operationB.QueueItemId));

            var resultC =
                Assert.Single(
                    result.Items.Where(x =>
                        x.QueueItemId ==
                        operationC.QueueItemId));

            Assert.Equal(
                SyncBatchItemStatus.Done,
                resultA.Status);

            Assert.Equal(
                SyncBatchItemStatus.Failed,
                resultB.Status);

            Assert.Equal(
                SyncBatchItemStatus.Done,
                resultC.Status);

            Assert.Contains(
                calls,
                call =>
                    call.Count == 3);

            Assert.Contains(
                calls,
                call =>
                    call.Count == 1 &&
                    call[0].QueueItemId ==
                    operationB.QueueItemId);
        }

        [Fact]
        public async Task ProcessAsync_ShouldProcessEntityGroupsInDependencyOrder()
        {
            // Arrange
            var executionOrder = new List<string>();

            var legacySaleHandler =
                new Mock<ISyncOperationHandler>();

            legacySaleHandler
                .SetupGet(x => x.EntityName)
                .Returns("Sale");

            var batchCustomerHandler =
                new Mock<ISyncBatchOperationHandler>();

            batchCustomerHandler
                .SetupGet(x => x.EntityName)
                .Returns("Customer");

            var batchProductHandler =
                new Mock<ISyncBatchOperationHandler>();

            batchProductHandler
                .SetupGet(x => x.EntityName)
                .Returns("Product");

            var batchStockMovementHandler =
                new Mock<ISyncBatchOperationHandler>();

            batchStockMovementHandler
                .SetupGet(x => x.EntityName)
                .Returns("StockMovement");

            var batchExecutorMock =
                new Mock<ISyncBatchOperationExecutor>();

            batchExecutorMock
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyList<SyncBatchOperationRequest>>(),
                    It.IsAny<ISyncBatchOperationHandler>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Guid,
                         IReadOnlyList<SyncBatchOperationRequest>,
                         ISyncBatchOperationHandler,
                         CancellationToken>(
                    (batchId, operations, handler, cancellationToken) =>
                    {
                        executionOrder.Add(handler.EntityName);

                        IReadOnlyList<SyncBatchItemResult> results =
                            operations
                                .Select(operation =>
                                    new SyncBatchItemResult
                                    {
                                        QueueItemId = operation.QueueItemId,
                                        ClientOperationId = operation.ClientOperationId,
                                        Status = SyncBatchItemStatus.Done
                                    })
                                .ToArray();

                        return Task.FromResult(results);
                    });

            var operationExecutorMock =
                new Mock<ISyncOperationExecutor>();

            operationExecutorMock
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<SyncBatchOperationRequest>(),
                    It.IsAny<ISyncOperationHandler>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Guid,
                         SyncBatchOperationRequest,
                         ISyncOperationHandler,
                         CancellationToken>(
                    (batchId, operation, handler, cancellationToken) =>
                    {
                        executionOrder.Add(handler.EntityName);

                        return Task.FromResult(
                            new SyncBatchItemResult
                            {
                                QueueItemId = operation.QueueItemId,
                                ClientOperationId = operation.ClientOperationId,
                                Status = SyncBatchItemStatus.Done
                            });
                    });

            var loggerMock =
                new Mock<ILogger<SyncBatchService>>();

            var service =
                new SyncBatchService(
                    new[]
                    {
                legacySaleHandler.Object
                    },
                    new[]
                    {
                batchCustomerHandler.Object,
                batchProductHandler.Object,
                batchStockMovementHandler.Object
                    },
                    operationExecutorMock.Object,
                    batchExecutorMock.Object,
                    loggerMock.Object);

            var request =
                new SyncBatchRequest
                {
                    BatchId = Guid.NewGuid(),
                    Operations =
                    [
                        CreateOperation("StockMovement"),
                CreateOperation("Sale"),
                CreateOperation("Product"),
                CreateOperation("Customer")
                    ]
                };

            // Act
            await service.ProcessAsync(request);

            // Assert
            Assert.Equal(
                new[]
                {
            "Customer",
            "Product",
            "Sale",
            "StockMovement"
                },
                executionOrder);
        }

        private static SyncBatchOperationRequest CreateOperation(string entityName)
        {
            using var payload =
                JsonDocument.Parse(
                    """
            {
                "test": true
            }
            """);

            return new SyncBatchOperationRequest
            {
                QueueItemId = Guid.NewGuid(),
                ClientOperationId = Guid.NewGuid(),
                LocalEntityId = Guid.NewGuid(),
                EntityName = entityName,
                Operation = "Create",
                Payload = payload.RootElement.Clone()
            };
        }

        private static SyncBatchOperationRequest CreateOperation()
        {
            using var payload =
                JsonDocument.Parse(
                    """
                    {
                        "name": "Test product"
                    }
                    """);

            return new SyncBatchOperationRequest
            {
                QueueItemId =
                    Guid.NewGuid(),

                ClientOperationId =
                    Guid.NewGuid(),

                LocalEntityId =
                    Guid.NewGuid(),

                EntityName =
                    "Product",

                Operation =
                    "Create",

                Payload =
                    payload.RootElement.Clone()
            };
        }
    }
}