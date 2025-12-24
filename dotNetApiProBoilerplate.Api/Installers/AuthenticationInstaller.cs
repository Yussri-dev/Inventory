using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Inventory.Api.Installers
{
    public static class AuthenticationInstaller
    {
        public static WebApplicationBuilder InstallAuthentication(this WebApplicationBuilder builder)
        {
            var config = builder.Configuration;
            var keyBytes = Convert.FromBase64String(config["Jwt:Key"]!);
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,

                        ValidateAudience = true,

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,

                        ValidIssuer = config["Jwt:Issuer"],

                        ValidAudience = config["Jwt:Audience"],

                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            context.HandleResponse();

                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";

                            var result = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                error = "Unauthorized",
                                message = "Invalid or missing authentication token"
                            });

                            return context.Response.WriteAsync(result);
                        },

                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"Authentication failed: {context.Exception.Message}");

                            return Task.CompletedTask;
                        }
                    };
                });

            return builder;
        }
    }
}
