// API versioning library
// Provides support for multiple API versions in ASP.NET Core
using Asp.Versioning;

namespace Inventory.Api.Installers
{
    // Static installer class following the Installer pattern
    // Purpose: centralize API versioning configuration
    public static class VersioningInstaller
    {
        // Extension method used in Program.cs
        // Registers and configures API versioning and versioned API explorer
        public static WebApplicationBuilder InstallVersioning(this WebApplicationBuilder builder)
        {
            // Register API versioning services
            builder.Services.AddApiVersioning(options =>
            {
                // Define the default API version (v1.0)
                // Used when no version is specified by the client
                options.DefaultApiVersion = new ApiVersion(1, 0);

                // Assume the default version if the client does not specify one
                options.AssumeDefaultVersionWhenUnspecified = true;

                // Include supported and deprecated API versions in response headers
                options.ReportApiVersions = true;

                // Configure how the API version is read from incoming requests
                options.ApiVersionReader = ApiVersionReader.Combine(
                    // Read version from the URL segment: /api/v{version}/...
                    new UrlSegmentApiVersionReader(),

                    // Also allow version to be specified via HTTP header
                    // Example: X-Api-Version: 1.0
                    new HeaderApiVersionReader("X-Api-Version")
                );
            })
            // Register API explorer for versioned Swagger support
            .AddApiExplorer(options =>
            {
                // Format group names as: v1, v1.0, v2, etc.
                options.GroupNameFormat = "'v'VVV";

                // Automatically replace {version} in route templates
                options.SubstituteApiVersionInUrl = true;
            });

            // Return builder to allow fluent chaining of installers
            return builder;
        }
    }
}
