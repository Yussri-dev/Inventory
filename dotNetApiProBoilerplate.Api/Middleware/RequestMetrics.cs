// Provides HTTP status code definitions (400, 404, 500, etc.)

// JSON serialization utilities

// Import custom domain/service exceptions
// These exceptions are thrown by the service layer
using System.Collections.Concurrent;

namespace Inventory.Api.Middleware
{
    public static class RequestMetrics
    {
        public static ConcurrentDictionary<string, long> PerRoute = new();
    }
}
