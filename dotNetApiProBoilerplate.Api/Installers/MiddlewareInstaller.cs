// Import custom middleware components
// These middlewares extend the ASP.NET Core request pipeline
using Inventory.Api.Middleware;

namespace Inventory.Api.Installers
{
    // Static installer class following the Installer pattern
    // Purpose: centralize middleware registration and pipeline configuration
    public static class MiddlewareInstaller
    {
        // Extension method used during service registration phase
        // This is where middleware-related dependencies would be added to DI
        public static WebApplicationBuilder InstallMiddleware(this WebApplicationBuilder builder)
        {
            // Currently no middleware-specific services are required
            // This method exists for:
            // - Future extensibility
            // - Architectural consistency across installers
            // - Keeping Program.cs clean and predictable

            return builder;
        }

        // Extension method used during application pipeline configuration
        // This method wires middleware into the HTTP request pipeline
        public static WebApplication UseCustomMiddleware(this WebApplication app)
        {
            // Register global exception handling middleware
            // This must be one of the first middlewares in the pipeline
            // It catches all unhandled exceptions thrown by downstream components
            // and converts them into consistent API error responses
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Return the app to allow fluent chaining in Program.cs
            return app;
        }
    }
}
