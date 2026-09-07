using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncQueueItems_TenantId",
                table: "SyncQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_SyncQueueItems_TenantId_Status_CreatedAtUtc",
                table: "SyncQueueItems");

            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "SyncQueueItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAtUtc",
                table: "SyncQueueItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAtUtc",
                table: "SyncQueueItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_TenantId_ClientOperationId",
                table: "SyncQueueItems",
                columns: new[] { "TenantId", "ClientOperationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_TenantId_Status_CreatedAtUtc_NextAttemptAtUtc_Id",
                table: "SyncQueueItems",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc", "NextAttemptAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncQueueItems_TenantId_ClientOperationId",
                table: "SyncQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_SyncQueueItems_TenantId_Status_CreatedAtUtc_NextAttemptAtUtc_Id",
                table: "SyncQueueItems");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "SyncQueueItems");

            migrationBuilder.DropColumn(
                name: "LockedAtUtc",
                table: "SyncQueueItems");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "SyncQueueItems");

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_TenantId",
                table: "SyncQueueItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_TenantId_Status_CreatedAtUtc",
                table: "SyncQueueItems",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });
        }
    }
}
