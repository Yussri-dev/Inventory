// Provides HTTP status code definitions (400, 404, 500, etc.)
using System.Net;

// JSON serialization utilities
using System.Text.Json;

// Import custom domain/service exceptions
// These exceptions are thrown by the service layer
using Inventory.Services.Exceptions;

namespace Inventory.Api.Middleware
{
    // Custom middleware responsible for global exception handling
    // Catches all unhandled exceptions and converts them into JSON API responses
    public class ExceptionHandlingMiddleware
    {
        // Delegate pointing to the next middleware in the pipeline
        private readonly RequestDelegate _next;

        // Logger used to record exception details
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        // Environment information (Development, Production, etc.)
        // Used to control error detail exposure
        private readonly IHostEnvironment _environment;

        // Middleware constructor
        // Dependencies are injected by ASP.NET Core
        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        // Core middleware execution method
        // Called for every incoming HTTP request
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Pass the request to the next middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the exception with full stack trace
                _logger.LogError(
                    ex,
                    "An unhandled exception occurred: {Message}",
                    ex.Message
                );

                // Handle and convert the exception into a structured response
                await HandleExceptionAsync(context, ex);
            }
        }

        // Centralized exception-to-response mapping logic
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Ensure JSON response type
            context.Response.ContentType = "application/json";

            // Initialize a standardized error response object
            var response = new ErrorResponse
            {
                // Unique identifier for tracing the request
                TraceId = context.TraceIdentifier,

                // Request path that caused the error
                Instance = context.Request.Path
            };

            // Map known exception types to HTTP status codes and messages
            switch (exception)
            {
                // Validation errors (bad input, failed business rules)
                case ValidationException validationEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Status = (int)HttpStatusCode.BadRequest;
                    response.Title = "Validation Error";
                    response.Detail = validationEx.Message;
                    response.Errors = validationEx.Errors;
                    break;

                // Resource not found (entity does not exist)
                case NotFoundException notFoundEx:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Status = (int)HttpStatusCode.NotFound;
                    response.Title = "Resource Not Found";
                    response.Detail = notFoundEx.Message;
                    break;

                // Authentication failure
                case UnauthorizedAccessException unauthorizedEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Status = (int)HttpStatusCode.Unauthorized;
                    response.Title = "Unauthorized";
                    response.Detail = unauthorizedEx.Message;
                    break;

                // Authorization failure (user authenticated but not allowed)
                case ForbiddenException forbiddenEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    response.Status = (int)HttpStatusCode.Forbidden;
                    response.Title = "Forbidden";
                    response.Detail = forbiddenEx.Message;
                    break;

                // Conflict errors (duplicate resources, state conflicts)
                case ConflictException conflictEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Status = (int)HttpStatusCode.Conflict;
                    response.Title = "Conflict";
                    response.Detail = conflictEx.Message;
                    break;

                // Fallback for all unhandled exceptions
                default:
                    context.Response.StatusCode =
                        (int)HttpStatusCode.InternalServerError;
                    response.Status =
                        (int)HttpStatusCode.InternalServerError;
                    response.Title = "Internal Server Error";

                    // Show full error details only in Development
                    response.Detail = _environment.IsDevelopment()
                        ? exception.Message
                        : "An error occurred while processing your request.";

                    // Include stack trace only in Development environment
                    if (_environment.IsDevelopment())
                    {
                        response.StackTrace = exception.StackTrace;
                    }
                    break;
            }

            // Configure JSON serialization behavior
            var jsonOptions = new JsonSerializerOptions
            {
                // Use camelCase for JSON properties
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

                // Pretty-print JSON only in Development
                WriteIndented = _environment.IsDevelopment()
            };

            // Serialize the error response to JSON
            var json = JsonSerializer.Serialize(response, jsonOptions);

            // Write JSON response to the HTTP output
            await context.Response.WriteAsync(json);
        }
    }
}
