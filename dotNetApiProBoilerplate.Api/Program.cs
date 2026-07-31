using Inventory.Api.Installers;
using Inventory.Api.Middleware;
using QuestPDF.Infrastructure;

var builder =
    WebApplication.CreateBuilder(args);

QuestPDF.Settings.License =
    LicenseType.Community;

/*
 * Cette valeur est true par défaut pour le développement
 * classique avec Visual Studio.
 *
 * Dans Docker, elle sera définie à false avec une variable
 * d'environnement.
 */
var useHttpsRedirection =
    builder.Configuration.GetValue(
        "UseHttpsRedirection",
        true);

builder
    // Controllers and API explorer.
    .InstallRestApi()

    // Swagger/OpenAPI.
    .InstallSwagger()

    // Entity Framework Core database.
    .InstallDatabase()

    // Repositories, UnitOfWork and business services.
    .InstallServices()

    // JWT authentication.
    .InstallAuthentication()

    // ASP.NET Core Identity.
    .InstallIdentity()

    // API versioning.
    .InstallVersioning()

    // AutoMapper profiles.
    .InstallMapping()

    // MediatR handlers.
    .InstallMediatR()

    // Custom middleware services.
    .InstallMiddleware();

builder.Services.AddAuthorization();

var app =
    builder.Build();

/*
 * Global exception handler.
 */
app.UseMiddleware<ExceptionHandlingMiddleware>();

/*
 * Request metrics.
 */
app.Use(
    async (context, next) =>
    {
        var route =
            context.Request.Path.ToString();

        RequestMetrics.PerRoute.AddOrUpdate(
            route,
            1,
            (_, count) =>
                count + 1);

        await next();
    });

/*
 * Swagger remains enabled in Development.
 *
 * The Docker container will initially run with:
 * ASPNETCORE_ENVIRONMENT=Development
 */
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();

    app.UseSwaggerUI();
}

/*
 * Local Visual Studio:
 *     UseHttpsRedirection=true
 *
 * Docker:
 *     UseHttpsRedirection=false
 */
if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

/*
 * Simple endpoint used to check whether the Docker
 * container is running correctly.
 */
app.MapGet(
    "/health",
    () =>
        Results.Ok(
            new
            {
                status =
                    "healthy",

                application =
                    "Inventory.Api",

                utcTime =
                    DateTime.UtcNow
            }));

app.MapGet(
    "/metrics/requests",
    () =>
        Results.Ok(
            new
            {
                total =
                    RequestMetrics.PerRoute
            }));

app.Run();