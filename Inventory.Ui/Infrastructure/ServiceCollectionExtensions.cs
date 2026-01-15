

using Inventory.Ui.Services;
using Refit;
using System.Net;

namespace Inventory.Ui.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSecuredApi<T>(
            this IServiceCollection services,
            string baseUrl
        ) where T : class
        {
            var http = services
                .AddRefitClient<T>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri(baseUrl);
                    c.DefaultRequestVersion = HttpVersion.Version11;
                    c.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                });

#if DEBUG
            http.ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });
#endif

            http.AddHttpMessageHandler<AuthHeaderHandler>();

            return services;
        }

        public static IServiceCollection AddOpenApi<T>(
            this IServiceCollection services,
            string baseUrl
        ) where T : class
        {
            var http = services
                .AddRefitClient<T>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri(baseUrl);
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
