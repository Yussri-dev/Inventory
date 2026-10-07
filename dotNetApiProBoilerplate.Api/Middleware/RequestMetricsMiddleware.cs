using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace Inventory.Api.Middleware;

public sealed class RequestMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        var failed = false;
        try { await next(context); }
        catch { failed = true; throw; }
        finally
        {
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            RequestMetrics.Record(context.Request.Method, route, failed ? 500 : context.Response.StatusCode,
                timer.Elapsed.TotalMilliseconds, context.Request.ContentLength ?? 0, context.Response.ContentLength ?? 0);
        }
    }
}
