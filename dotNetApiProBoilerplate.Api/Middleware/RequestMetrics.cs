namespace Inventory.Api.Middleware;

public static class RequestMetrics
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, RouteMetric> Routes = new();
    public static DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    public const int MaximumRoutes = 128;
    public sealed record RouteMetric(long Requests, long Failures, double TotalMilliseconds, long KnownRequestBytes, long KnownResponseBytes);
    public static void Record(string method, string routeTemplate, int status, double milliseconds, long requestBytes, long responseBytes)
    {
        lock (Gate)
        {
            var key = $"{method} {routeTemplate} {status / 100}xx";
            if (!Routes.ContainsKey(key) && Routes.Count >= MaximumRoutes - 1) key = "other";
            var old = Routes.GetValueOrDefault(key) ?? new RouteMetric(0, 0, 0, 0, 0);
            Routes[key] = new(old.Requests + 1, old.Failures + (status >= 400 ? 1 : 0), old.TotalMilliseconds + milliseconds,
                old.KnownRequestBytes + requestBytes, old.KnownResponseBytes + responseBytes);
        }
    }
    public static object Snapshot()
    {
        lock (Gate) return new { StartedAtUtc, Scope = "process", ByteCounts = "Content-Length only; chunked bodies excluded",
            Routes = Routes.ToDictionary(x => x.Key, x => x.Value) };
    }
}
