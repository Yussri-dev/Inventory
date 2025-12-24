// OpenAPI base namespace
// Provides core OpenAPI primitives and helpers
using Microsoft.OpenApi;

// OpenAPI models used to describe API metadata and security
using Microsoft.OpenApi.Models;

namespace Inventory.Api.Installers
{
    // Static installer class following the Installer pattern
    // Purpose: centralize Swagger / OpenAPI configuration
    public static class SwaggerInstaller
    {
        // Extension method used in Program.cs
        // Registers Swagger services and configures OpenAPI metadata
        public static WebApplicationBuilder InstallSwagger(this WebApplicationBuilder builder)
        {
            // Register minimal OpenAPI support
            // Required for endpoint discovery and Swagger generation
            builder.Services.AddOpenApi();

            // Register and configure Swagger generator
            builder.Services.AddSwaggerGen(options =>
            {
                // Define the main Swagger document
                // This appears as the API definition in Swagger UI
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    // API title shown in Swagger UI
                    Title = "Inventory API",

                    // API version identifier
                    Version = "v1",

                    // Short description of the API purpose
                    Description = "API for Inventory"
                });

                // Define the JWT Bearer security scheme
                // This enables the "Authorize" button in Swagger UI
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    // Description shown in Swagger UI
                    // Explains how to provide the JWT token
                    Description =
                        "JWT Authorization header using the Bearer scheme. " +
                        "Enter 'Bearer' [space] and then your token in the text input below. " +
                        "Example: 'Bearer eyJhbGc...'",

                    // HTTP header name used to pass the token
                    Name = "Authorization",

                    // Location of the token (HTTP header)
                    In = ParameterLocation.Header,

                    // Security scheme type
                    // Http is required for bearer tokens
                    Type = SecuritySchemeType.Http,

                    // Authentication scheme name
                    Scheme = "bearer",

                    // Token format for documentation purposes
                    BearerFormat = "JWT"
                });

                // Apply the security scheme globally
                // All endpoints will require JWT authentication in Swagger UI
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        // Reference the previously defined Bearer scheme
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },

                        // No specific scopes required (JWT does not use OAuth scopes here)
                        Array.Empty<string>()
                    }
                });
            });

            // Return builder to allow fluent chaining of installers
            return builder;
        }
    }
}
