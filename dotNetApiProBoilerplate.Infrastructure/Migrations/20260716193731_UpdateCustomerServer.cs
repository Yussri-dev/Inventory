using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashSessionId",
                table: "CustomerTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientOperationId",
                table: "CustomerTransactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsCash",
                table: "CustomerTransactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_CashSessionId",
                table: "CustomerTransactions",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_SaleId",
                table: "CustomerTransactions",
                column: "SaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerTransactions_CashSessions_CashSessionId",
                table: "CustomerTransactions",
                column: "CashSessionId",
                principalTable: "CashSessions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerTransactions_Sales_SaleId",
                table: "CustomerTransactions",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerTransactions_CashSessions_CashSessionId",
                table: "CustomerTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerTransactions_Sales_SaleId",
                table: "CustomerTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTransactions_CashSessionId",
                table: "CustomerTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTransactions_SaleId",
                table: "CustomerTransactions");

            migrationBuilder.DropColumn(
                name: "CashSessionId",
                table: "CustomerTransactions");

            migrationBuilder.DropColumn(
                name: "ClientOperationId",
                table: "CustomerTransactions");

            migrationBuilder.DropColumn(
                name: "IsCash",
                table: "CustomerTransactions");
        }
    }
}
