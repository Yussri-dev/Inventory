using System.Text.Json;
using Inventory.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Inventory.Api.Tests;

public class RequestMetricsTests
{
    [Fact]
    public async Task Metrics_normalize_paths_count_failures_and_bound_memory()
    {
        var middleware = new RequestMetricsMiddleware(ctx => { ctx.Response.StatusCode = 409; return Task.CompletedTask; });
        for (var i = 0; i < 200; i++)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST"; context.Request.Path = "/sale/" + Guid.NewGuid();
            context.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse("sale/{id}"), 0, EndpointMetadataCollection.Empty, "Sale"));
            await middleware.InvokeAsync(context);
        }
        var snapshot = JsonSerializer.SerializeToElement(RequestMetrics.Snapshot());
        var entry = snapshot.GetProperty("Routes").GetProperty("POST sale/{id} 4xx");
        Assert.Equal(200, entry.GetProperty("Requests").GetInt64());
        Assert.Equal(200, entry.GetProperty("Failures").GetInt64());
        for (var i = 0; i < 1000; i++) RequestMetrics.Record("GET", "unknown-" + i, 404, 1, 0, 0);
        var bounded = JsonSerializer.SerializeToElement(RequestMetrics.Snapshot());
        Assert.True(bounded.GetProperty("Routes").EnumerateObject().Count() <= RequestMetrics.MaximumRoutes);
        Assert.Equal("process", bounded.GetProperty("Scope").GetString());
    }
}
