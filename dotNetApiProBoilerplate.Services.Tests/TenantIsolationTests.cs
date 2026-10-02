using System.Security.Claims;
using Inventory.Domain.Models;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services.Tests;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task Reads_are_scoped_even_when_context_model_is_reused()
    {
        await using var fixture = await Fixture.Create();
        await using var a = fixture.Context(fixture.A);
        await using var b = fixture.Context(fixture.B);
        Assert.Equal(new[] { fixture.CustomerA }, await a.Customers.Select(x => x.Id).ToArrayAsync());
        Assert.Equal(new[] { fixture.CustomerB }, await b.Customers.Select(x => x.Id).ToArrayAsync());
        Assert.Null(await new Repository<Customer>(a).GetByIdAsync(fixture.CustomerB));
        Assert.Single(await new ReadRepository<Customer>(a).Query().ToListAsync());
        Assert.Equal(1, await new Repository<Customer>(a).CountAsync());
    }

    [Fact]
    public async Task Tracked_foreign_entity_does_not_bypass_repository_read_filter()
    {
        await using var fixture = await Fixture.Create();
        await using var db = fixture.Context(fixture.A);
        db.Attach(new Customer { Id = fixture.CustomerB, TenantId = fixture.B, Name = "Foreign" });
        Assert.Null(await new Repository<Customer>(db).GetByIdAsync(fixture.CustomerB));
    }

    [Fact]
    public async Task Missing_or_unauthenticated_tenant_cannot_read_business_data()
    {
        await using var fixture = await Fixture.Create();
        await using var missing = fixture.Context(null);
        await using var anonymous = fixture.Context(fixture.A, authenticated: false);
        Assert.Empty(await missing.Customers.ToListAsync());
        Assert.Empty(await anonymous.Customers.ToListAsync());
        missing.Customers.Add(new Customer { Id = Guid.NewGuid(), TenantId = fixture.A, Name = "Blocked" });
        await Assert.ThrowsAsync<TenantIsolationException>(() => missing.SaveChangesAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Foreign_inserts_are_rejected_before_any_changes_are_saved(bool synchronous)
    {
        await using var fixture = await Fixture.Create();
        await using var db = fixture.Context(fixture.A);
        db.Customers.Add(new Customer { Id = Guid.NewGuid(), TenantId = fixture.A, Name = "Allowed" });
        db.Customers.Add(new Customer { Id = Guid.NewGuid(), TenantId = fixture.B, Name = "Blocked" });
        if (synchronous) Assert.Throws<TenantIsolationException>(() => db.SaveChanges());
        else await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
        await using var check = fixture.Context(fixture.A);
        Assert.Single(await check.Customers.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Forged_owner_on_detached_update_or_delete_is_rejected(bool delete)
    {
        await using var fixture = await Fixture.Create();
        await using var db = fixture.Context(fixture.A);
        var forged = new Customer { Id = fixture.CustomerB, TenantId = fixture.A, Name = "Changed" };
        if(delete) db.Remove(forged); else db.Update(forged);
        await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
        await using var check = fixture.Context(fixture.B);
        Assert.Equal("B customer", (await check.Customers.SingleAsync()).Name);
    }

    [Fact]
    public async Task Existing_owner_cannot_be_changed()
    {
        await using var fixture = await Fixture.Create();
        await using var db = fixture.Context(fixture.A);
        var customer = await db.Customers.SingleAsync();
        customer.TenantId = fixture.B;
        await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Foreign_customer_relationship_is_rejected_even_for_superadmin(bool superAdmin)
    {
        await using var fixture = await Fixture.Create();
        await using var db = fixture.Context(fixture.A, superAdmin);
        db.CustomerTransactions.Add(new CustomerTransaction {
            Id = Guid.NewGuid(), TenantId = fixture.A, CustomerId = fixture.CustomerB,
            Type = "Payment", Amount = 10, Description = "Test"
        });
        await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Same_tenant_relationship_and_updates_work()
    {
        await using var fixture = await Fixture.Create();
        await using var db = fixture.Context(fixture.A);
        var customer = await db.Customers.SingleAsync();
        customer.Name = "Updated";
        db.CustomerTransactions.Add(new CustomerTransaction {
            Id = Guid.NewGuid(), TenantId = fixture.A, CustomerId = fixture.CustomerA,
            Type = "Payment", Amount = 10, Description = "Test"
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Equal("Updated", (await db.Customers.SingleAsync()).Name);
        Assert.Single(await db.CustomerTransactions.ToListAsync());
    }

    [Fact]
    public async Task All_business_entity_types_have_a_filter_and_global_catalog_does_not()
    {
        await using var fixture = await Fixture.Create();
        await using var db = fixture.Context(fixture.A);
        var owned = db.Model.GetEntityTypes().Where(t => t.FindProperty("TenantId")?.ClrType == typeof(Guid)).ToArray();
        Assert.True(owned.Length >= 30);
        Assert.All(owned, type => Assert.NotNull(type.GetQueryFilter()));
        Assert.Null(db.Model.FindEntityType(typeof(ApplicationUser))!.GetQueryFilter());
        Assert.Null(db.Model.FindEntityType(typeof(Inventory.Domain.Entities.ProductCatalog))!.GetQueryFilter());
    }

    [Fact]
    public async Task Superadmin_reads_all_and_provisioning_scope_is_restored()
    {
        await using var fixture = await Fixture.Create();
        await using var admin = fixture.Context(null, superAdmin: true);
        Assert.Equal(2, await admin.Customers.CountAsync());
        using(admin.BeginProductProvisioning(fixture.A))
            Assert.Single(await admin.Customers.ToListAsync());
        Assert.Equal(2, await admin.Customers.CountAsync());
        await using var user = fixture.Context(fixture.A);
        Assert.Throws<TenantIsolationException>(() => user.BeginProductProvisioning(fixture.B));
    }

    [Fact]
    public async Task Anonymous_registration_provisions_only_the_verified_company_and_restores_access()
    {
        await using var fixture = await Fixture.Create();
        var userId = Guid.NewGuid();
        await using (var seed = fixture.Context(null, superAdmin: true))
        {
            seed.Users.Add(new ApplicationUser { Id = userId, TenantId = fixture.A, UserName = "test" });
            var category = new ProductCategory { Id = Guid.NewGuid(), Name = "Shared category" };
            seed.Add(category);
            seed.ProductCatalogs.Add(new ProductCatalog { Id = Guid.NewGuid(), CategoryId = category.Id,
                Name = "Shared product", InternalCode = "TEST" });
            foreach (var entry in seed.ChangeTracker.Entries())
                foreach (var property in entry.Properties)
                    if (property.Metadata.ClrType == typeof(string) && !property.Metadata.IsNullable && property.CurrentValue == null)
                        property.CurrentValue = "Test";
            await seed.SaveChangesAsync();
        }
        await using var anonymous = fixture.Context(null, authenticated: false);
        var provisioning = new ProductProvisioningService(anonymous,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProductProvisioningService>.Instance);
        Assert.Equal(1, await provisioning.ProvisionCatalogProductsAsync(fixture.A, userId));
        Assert.Equal(0, await provisioning.ProvisionCatalogProductsAsync(fixture.A, userId));
        Assert.Empty(await anonymous.Products.ToListAsync());
        await Assert.ThrowsAsync<TenantIsolationException>(() =>
            provisioning.ProvisionCatalogProductsAsync(fixture.B, userId));
        await using var a = fixture.Context(fixture.A);
        await using var b = fixture.Context(fixture.B);
        Assert.Single(await a.Products.ToListAsync());
        Assert.Empty(await b.Products.ToListAsync());
        Assert.Single(await b.ProductCatalogs.ToListAsync());
    }

    [Fact]
    public void PostgreSql_query_contains_parameterized_tenant_filter()
    {
        var access = new Moq.Mock<ITenantDataAccess>();
        access.SetupGet(x => x.IsAuthenticated).Returns(true);
        access.SetupGet(x => x.TenantId).Returns(Guid.NewGuid());
        using var db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options, access.Object);
        var sql = db.Customers.ToQueryString();
        Assert.Contains("WHERE", sql);
        Assert.Contains("CurrentDataTenantId", sql);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public Guid A { get; } = Guid.NewGuid();
        public Guid B { get; } = Guid.NewGuid();
        public Guid CustomerA { get; } = Guid.NewGuid();
        public Guid CustomerB { get; } = Guid.NewGuid();

        public InventoryDbContext Context(Guid? tenant, bool superAdmin = false, bool authenticated = true)
        {
            var claims = new List<Claim>();
            if(tenant.HasValue) claims.Add(new("TenantId", tenant.Value.ToString()));
            if(superAdmin) claims.Add(new(ClaimTypes.Role, "SuperAdmin"));
            var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "test" : null))
            }};
            return new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlite(connection).Options, new Inventory.Services.Context.TenantDataAccess(accessor));
        }

        public static async Task<Fixture> Create()
        {
            var f = new Fixture();
            await f.connection.OpenAsync();
            await using var db = f.Context(null, superAdmin: true);
            await db.Database.EnsureCreatedAsync();
            db.Tenants.AddRange(new Tenant { Id = f.A }, new Tenant { Id = f.B });
            db.Customers.AddRange(new Customer { Id=f.CustomerA, TenantId=f.A, Name="A customer" },
                new Customer { Id=f.CustomerB, TenantId=f.B, Name="B customer" });
            foreach(var entry in db.ChangeTracker.Entries())
                foreach(var property in entry.Properties)
                    if(property.Metadata.ClrType == typeof(string) && !property.Metadata.IsNullable && property.CurrentValue == null)
                        property.CurrentValue = "Test-"+Guid.NewGuid().ToString("N");
            await db.SaveChangesAsync();
            return f;
        }

        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }
}
