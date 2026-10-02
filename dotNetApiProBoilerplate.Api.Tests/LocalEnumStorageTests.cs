using Inventory.Dto.Enums;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Tests;

public class LocalEnumStorageTests
{
    [Fact]
    public async Task Enums_keep_existing_text_columns_and_read_legacy_values()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new PosLocalDbContext(new DbContextOptionsBuilder<PosLocalDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var tenant = Guid.NewGuid();
        var sale = new LocalSale { TenantId = tenant, LocalInvoiceNumber = "S1" };
        Assert.Equal(SaleStatus.Completed, sale.Status);
        var payment = new LocalPayment { TenantId = tenant, LocalSaleId = sale.Id, Method = PaymentMethod.Credit, Amount = 12 };
        var returned = new LocalReturn { TenantId = tenant, LocalSaleId = sale.Id, LocalReturnNumber = "R1", RefundMethod = RefundMethod.Cash };
        db.AddRange(sale, payment, returned);
        await db.SaveChangesAsync();
        foreach (var (type, property) in new[] { (typeof(LocalSale), "Status"), (typeof(LocalPayment), "Method"), (typeof(LocalReturn), "RefundMethod"), (typeof(LocalStockMovement), "Type") })
        {
            var converter = db.Model.FindEntityType(type)!.FindProperty(property)!.GetTypeMapping().Converter;
            Assert.NotNull(converter);
            Assert.Equal(typeof(string), converter!.ProviderClrType);
        }
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Method FROM Payments";
        Assert.Equal("Credit", await command.ExecuteScalarAsync());
        command.CommandText = "UPDATE Sales SET Status = 'Pending'";
        await command.ExecuteNonQueryAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(SaleStatus.Pending, (await db.Sales.SingleAsync()).Status);
        Assert.Equal(RefundMethod.Cash, (await db.Returns.SingleAsync()).RefundMethod);
        Assert.Single(await db.Sales.Where(x => x.Status == SaleStatus.Pending).ToListAsync());
    }
}
