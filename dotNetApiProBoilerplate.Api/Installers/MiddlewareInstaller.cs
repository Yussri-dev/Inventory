using Inventory.Api.Middleware;

namespace Inventory.Api.Installers
{
    public static class MiddlewareInstaller
    {
        public static WebApplicationBuilder InstallMiddleware(this WebApplicationBuilder builder)
        {

            return builder;
        }

        public static WebApplication UseCustomMiddleware(this WebApplication app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            return app;
        }
    }
}
