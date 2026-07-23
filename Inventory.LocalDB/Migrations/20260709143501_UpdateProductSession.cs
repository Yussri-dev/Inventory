using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_ServerId",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "PayloadJson",
                table: "SyncQueueItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ServerId",
                table: "Products",
                column: "ServerId",
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SyncStatus",
                table: "Products",
                column: "SyncStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_ServerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SyncStatus",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "PayloadJson",
                table: "SyncQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ServerId",
                table: "Products",
                column: "ServerId",
                unique: true);
        }
    }
}
