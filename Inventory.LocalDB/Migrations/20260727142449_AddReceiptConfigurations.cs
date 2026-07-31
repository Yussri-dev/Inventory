using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Locale",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "fr-BE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "CurrencySymbol",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "€",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptConfigurationUpdatedAtUtc",
                table: "StoreProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptCurrencyCode",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<string>(
                name: "ReceiptDefaultCashierName",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptExtraAddressLine",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptHeaderTagLine",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ReceiptLogoBytes",
                table: "StoreProfiles",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptLogoContentType",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptLogoFileName",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptSocialLine",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptConfigurationUpdatedAtUtc",
                table: "StoreProfiles");

            migrationBuilder.DropColumn(
                name: "ReceiptCurrencyCode",
                table: "StoreProfiles");

            migrationBuilder.DropColumn(
                name: "ReceiptDefaultCashierName",
                table: "StoreProfiles");

            migrationBuilder.DropColumn(
                name: "ReceiptExtraAddressLine",
                table: "StoreProfiles");

            migrationBuilder.DropColumn(
                name: "ReceiptHeaderTagLine",
                table: "StoreProfiles");

            migrationBuilder.DropColumn(
                name: "ReceiptLogoBytes",
                table: "StoreProfiles");

            migrationBuilder.DropColumn(
                name: "ReceiptLogoContentType",
                table: "StoreProfiles");

            migrationBuilder.DropColumn(
                name: "ReceiptLogoFileName",
                table: "StoreProfiles");

            migrationBuilder.DropColumn(
                name: "ReceiptSocialLine",
                table: "StoreProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "Locale",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 10,
                oldDefaultValue: "fr-BE");

            migrationBuilder.AlterColumn<string>(
                name: "CurrencySymbol",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 10,
                oldDefaultValue: "€");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "StoreProfiles",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 3,
                oldDefaultValue: "EUR");
        }
    }
}
