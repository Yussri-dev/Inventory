using Inventory.Api.Installers;

using Inventory.Api.Middleware;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

builder
    // Registers controllers and API explorer
    .InstallRestApi()

    .InstallSwagger()

    .InstallDatabase()

    // Registers repositories, UnitOfWork, and business services
    .InstallServices()

    // Registers JWT authentication configuration
    .InstallAuthentication()

    // Registers ASP.NET Core Identity (users, passwords, tokens)
    .InstallIdentity()

    // Registers API versioning and versioned API explorer
    .InstallVersioning()

    // Registers AutoMapper and scans for mapping profiles
    .InstallMapping()

    // Registers MediatR
    .InstallMediatR()

    // Registers middleware-related services (if any)
    .InstallMiddleware();

// Register authorization services
// Enables [Authorize] and policy-based authorization
builder.Services.AddAuthorization();

// Build the WebApplication
// At this point, the DI container is finalized
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();



app.Use(async (context, next) =>
{
    var route = context.Request.Path.ToString();

    RequestMetrics.PerRoute.AddOrUpdate(route, 1, (_, count) => count + 1);

    await next();
});

// Development-only configuration
if (app.Environment.IsDevelopment())
{
    // Map OpenAPI endpoints (minimal OpenAPI support)
    app.MapOpenApi();

    // Enable Swagger middleware
    app.UseSwagger();

    // Enable Swagger UI for interactive API exploration
    app.UseSwaggerUI();
}

// Enforce HTTPS redirection
// Automatically redirects HTTP requests to HTTPS
app.UseHttpsRedirection();

// Enable authentication middleware
// Validates JWT tokens and sets HttpContext.User
app.UseAuthentication();

// Enable authorization middleware
// Enforces access rules defined by [Authorize] attributes
app.UseAuthorization();

// Map controller endpoints
// Activates attribute-based routing
app.MapControllers();

app.MapGet("/metrics/requests", () =>
{
    return new { total = RequestMetrics.PerRoute };
});

// Start the HTTP server and begin listening for requests
app.Run();
