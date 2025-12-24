// OpenAPI base namespace
// Provides core OpenAPI primitives and helpers
using Microsoft.OpenApi;

using Microsoft.OpenApi.Models;

namespace Inventory.Api.Installers
{
    public static class SwaggerInstaller
    {
        public static WebApplicationBuilder InstallSwagger(this WebApplicationBuilder builder)
        {
            builder.Services.AddOpenApi();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Inventory API",

                    Version = "v1",

                    Description = "API for Inventory"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description =
                        "JWT Authorization header using the Bearer scheme. " +
                        "Enter 'Bearer' [space] and then your token in the text input below. " +
                        "Example: 'Bearer eyJhbGc...'",

                    Name = "Authorization",

                    In = ParameterLocation.Header,

                    Type = SecuritySchemeType.Http,

                    Scheme = "bearer",

                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },

                        Array.Empty<string>()
                    }
                });
            });

            return builder;
        }
    }
}
