using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLocalSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OfflinePasswordHash",
                table: "UserSessions",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OfflinePasswordIterations",
                table: "UserSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OfflinePasswordSalt",
                table: "UserSessions",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfflinePasswordHash",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "OfflinePasswordIterations",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "OfflinePasswordSalt",
                table: "UserSessions");
        }
    }
}
