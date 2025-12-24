namespace Inventory.Api.Installers
{
    // Static installer class following the Installer pattern
    // Purpose: configure core REST API services
    public static class RestApiInstaller
    {
        // Extension method used in Program.cs
        // Registers all services required to expose a RESTful API
        public static WebApplicationBuilder InstallRestApi(this WebApplicationBuilder builder)
        {
            // Register MVC controllers
            // Enables attribute-based routing and controller/action discovery
            builder.Services.AddControllers();

            // Register the API explorer
            // Used by Swagger / OpenAPI to discover endpoints and metadata
            builder.Services.AddEndpointsApiExplorer();

            // Return builder to allow fluent chaining of installers
            return builder;
        }
    }
}
