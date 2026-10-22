using Inventory.Dto.Sync.Requests;
using Inventory.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;

namespace dotNetApiProBoilerplate.Services.Tests
{
    public sealed class SyncBatchOperationExecutorTests
    {
        [Fact]
        public void IsUniqueConstraintViolation_ShouldReturnTrue_ForIdempotencyIndex()
        {
            // Arrange
            var postgresException =
                CreateUniqueViolation(
                    "IX_SyncOperationRecords_TenantId_ClientOperationId");

            var exception =
                new DbUpdateException(
                    "Database update failed.",
                    postgresException);

            // Act
            var result =
                SyncBatchOperationExecutor
                    .IsUniqueConstraintViolation(
                        exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUniqueConstraintViolation_ShouldReturnFalse_ForAnotherUniqueIndex()
        {
            // Arrange
            var postgresException =
                CreateUniqueViolation(
                    "IX_Products_TenantId_Barcode");

            var exception =
                new DbUpdateException(
                    "Database update failed.",
                    postgresException);

            // Act
            var result =
                SyncBatchOperationExecutor
                    .IsUniqueConstraintViolation(
                        exception);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CalculatePayloadHash_ShouldReturnSameHash_WhenJsonPropertyOrderIsDifferent()
        {
            // Arrange
            using var document1 =
                JsonDocument.Parse(
                    """
                    {
                        "name": "Test",
                        "price": 10
                    }
                    """);

            using var document2 =
                JsonDocument.Parse(
                    """
                    {
                        "price": 10,
                        "name": "Test"
                    }
                    """);

            var localEntityId =
                Guid.NewGuid();

            var operation1 =
                new SyncBatchOperationRequest
                {
                    QueueItemId = Guid.NewGuid(),
                    ClientOperationId = Guid.NewGuid(),
                    LocalEntityId = localEntityId,
                    ServerEntityId = null,
                    EntityName = "Product",
                    Operation = "Create",
                    Payload =
                        document1.RootElement.Clone(),
                    CreatedAtUtc =
                        DateTime.UtcNow
                };

            var operation2 =
                new SyncBatchOperationRequest
                {
                    QueueItemId = Guid.NewGuid(),
                    ClientOperationId = Guid.NewGuid(),
                    LocalEntityId = localEntityId,
                    ServerEntityId = null,
                    EntityName = "Product",
                    Operation = "Create",
                    Payload =
                        document2.RootElement.Clone(),
                    CreatedAtUtc =
                        DateTime.UtcNow
                };

            // Act
            var hash1 =
                SyncBatchOperationExecutor
                    .CalculatePayloadHash(
                        operation1);

            var hash2 =
                SyncBatchOperationExecutor
                    .CalculatePayloadHash(
                        operation2);

            // Assert
            Assert.Equal(
                hash1,
                hash2);
        }

        private static PostgresException CreateUniqueViolation(
            string constraintName)
        {
            return new PostgresException(
                messageText: "duplicate key value violates unique constraint",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UniqueViolation,
                constraintName: constraintName);
        }
    }
}