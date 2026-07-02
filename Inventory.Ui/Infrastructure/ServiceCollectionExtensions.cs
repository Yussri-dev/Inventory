using Inventory.Ui.Services;
using Refit;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inventory.Ui.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        private static RefitSettings CreateRefitSettings()
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            jsonOptions.Converters.Add(
                new JsonStringEnumConverter());

            return new RefitSettings
            {
                ContentSerializer =
                    new SystemTextJsonContentSerializer(jsonOptions)
            };
        }

        public static IServiceCollection AddSecuredApi<T>(
            this IServiceCollection services,
            string baseUrl,
            bool withRefresh = true
        ) where T : class
        {
            var refitSettings = CreateRefitSettings();

            var http = services
                .AddRefitClient<T>(refitSettings)
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri(baseUrl);
                    c.DefaultRequestVersion = HttpVersion.Version11;
                    c.DefaultVersionPolicy =
                        HttpVersionPolicy.RequestVersionOrLower;
                });

#if DEBUG
            http.ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });
#endif

            if (withRefresh)
            {
                http.AddHttpMessageHandler<RefreshTokenHandler>();
                http.AddHttpMessageHandler<AuthExpiredHandler>();
            }

            http.AddHttpMessageHandler<AuthHeaderHandler>();

            return services;
        }

        public static IServiceCollection AddOpenApi<T>(
            this IServiceCollection services,
            string baseUrl
        ) where T : class
        {
            var refitSettings = CreateRefitSettings();

            var http = services
                .AddRefitClient<T>(refitSettings)
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri(baseUrl);
                    c.DefaultRequestVersion = HttpVersion.Version11;
                    c.DefaultVersionPolicy =
                        HttpVersionPolicy.RequestVersionOrLower;
                });

#if DEBUG
            http.ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });
#endif

            return services;
        }
    }
}