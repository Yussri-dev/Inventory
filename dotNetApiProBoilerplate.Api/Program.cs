// Import all installer extension methods
// Each installer encapsulates a specific infrastructure concern
using Inventory.Api.Installers;

// Import custom middleware explicitly
// Used here to register middleware in the request pipeline
using Inventory.Api.Middleware;

// Create the WebApplicationBuilder
// This is the entry point for configuring services and middleware
var builder = WebApplication.CreateBuilder(args);

// Apply installer pattern to keep Program.cs clean and readable
// Each call configures a specific part of the application
builder
    // Registers controllers and API explorer
    .InstallRestApi()

    // Registers Swagger / OpenAPI configuration
    .InstallSwagger()

    // Registers Entity Framework Core and database configuration
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

// Register global exception handling middleware
// This should be one of the first middlewares in the pipeline
// It catches all unhandled exceptions and returns standardized JSON errors
app.UseMiddleware<ExceptionHandlingMiddleware>();

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

// Start the HTTP server and begin listening for requests
app.Run();
