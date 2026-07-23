using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReturnServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId",
                table: "Returns");

            migrationBuilder.AlterColumn<string>(
                name: "RefundMethod",
                table: "Returns",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "CashSessionId",
                table: "Returns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientOperationId",
                table: "Returns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SaleLineId",
                table: "ReturnLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Returns_CashSessionId",
                table: "Returns",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_CashSessionId",
                table: "Returns",
                columns: new[] { "TenantId", "CashSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_ClientOperationId",
                table: "Returns",
                columns: new[] { "TenantId", "ClientOperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_SaleLineId",
                table: "ReturnLines",
                column: "SaleLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnLines_SaleLines_SaleLineId",
                table: "ReturnLines",
                column: "SaleLineId",
                principalTable: "SaleLines",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Returns_CashSessions_CashSessionId",
                table: "Returns",
                column: "CashSessionId",
                principalTable: "CashSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnLines_SaleLines_SaleLineId",
                table: "ReturnLines");

            migrationBuilder.DropForeignKey(
                name: "FK_Returns_CashSessions_CashSessionId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_CashSessionId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_CashSessionId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_ClientOperationId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_SaleLineId",
                table: "ReturnLines");

            migrationBuilder.DropColumn(
                name: "CashSessionId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "ClientOperationId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "SaleLineId",
                table: "ReturnLines");

            migrationBuilder.AlterColumn<int>(
                name: "RefundMethod",
                table: "Returns",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId",
                table: "Returns",
                column: "TenantId");
        }
    }
}
