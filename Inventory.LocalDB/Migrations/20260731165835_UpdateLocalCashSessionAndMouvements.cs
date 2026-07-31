using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLocalCashSessionAndMouvements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReturnLocalId",
                table: "CustomerTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReturnServerId",
                table: "CustomerTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowCredit",
                table: "Customers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasUnlimitedCredit",
                table: "Customers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnLocalId",
                table: "CustomerTransactions");

            migrationBuilder.DropColumn(
                name: "ReturnServerId",
                table: "CustomerTransactions");

            migrationBuilder.DropColumn(
                name: "AllowCredit",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "HasUnlimitedCredit",
                table: "Customers");
        }
    }
}
