using System.Net;
using System.Net.Http.Json;
using Inventory.Dto.Sync;
using Inventory.Dto.Products.Results;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Tests;

public sealed partial class PostgreSqlSyncTests
{
    [Fact]
    public async Task Snapshot_returns_all_equal_name_customers_beyond_old_page_limit()
    {
        var store = await fixture.Seed();
        await using (var db = fixture.Database())
        {
            for (int i = 0; i < 251; i++) db.Customers.Add(new Inventory.Domain.Entities.Customer {
                Id = Guid.NewGuid(), TenantId = store.TenantId, Name = "Same name", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        using var response = await fixture.Send(store, "/api/sync-downloads/customers", null, method: HttpMethod.Get);
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<SyncDownload<Inventory.Dto.Customers.Results.CustomerResult>>();
        Assert.Equal(251, snapshot!.Items.Count);
        Assert.Equal(251, snapshot.Items.Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public async Task Download_snapshot_is_authenticated_store_scoped_and_includes_tombstones()
    {
        var a = await fixture.Seed(); var b = await fixture.Seed();
        using var anonymous = await fixture.Client.GetAsync("/api/sync-downloads/products");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var response = await fixture.Send(a, "/api/sync-downloads/products", null, method: HttpMethod.Get);
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<SyncDownload<ProductResult>>();
        Assert.Equal(a.ProductId, Assert.Single(snapshot!.Items).Id);
        Assert.DoesNotContain(snapshot.Items, x => x.Id == b.ProductId);
        await using (var db = fixture.Database())
        {
            var product = await db.Products.SingleAsync(x => x.Id == a.ProductId);
            product.IsDeleted = true; await db.SaveChangesAsync();
        }
        using var deletedResponse = await fixture.Send(a, "/api/sync-downloads/products", null, method: HttpMethod.Get);
        deletedResponse.EnsureSuccessStatusCode();
        var deleted = await deletedResponse.Content.ReadFromJsonAsync<SyncDownload<ProductResult>>();
        Assert.Empty(deleted!.Items); Assert.Equal(a.ProductId, Assert.Single(deleted.DeletedIds));
    }

    [Theory]
    [InlineData("customers")]
    [InlineData("suppliers")]
    [InlineData("stocks")]
    [InlineData("damages")]
    [InlineData("catalogs")]
    public async Task Snapshot_projections_execute_on_PostgreSQL(string entity)
    {
        var store = await fixture.Seed();
        using var response = await fixture.Send(store, "/api/sync-downloads/" + entity, null, method: HttpMethod.Get);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(System.Text.Json.JsonValueKind.Array, json.GetProperty("items").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, json.GetProperty("deletedIds").ValueKind);
    }
}
