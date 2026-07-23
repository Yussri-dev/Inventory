using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCashSessionServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId1",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId1",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientOperationId",
                table: "CashSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientOperationId",
                table: "CashSessions");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId1",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId1",
                table: "AspNetUsers",
                column: "TenantId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId1",
                table: "AspNetUsers",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");
        }
    }
}
