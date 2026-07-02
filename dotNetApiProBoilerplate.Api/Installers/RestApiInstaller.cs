using System.Text.Json.Serialization;

namespace Inventory.Api.Installers
{
    public static class RestApiInstaller
    {
        public static WebApplicationBuilder InstallRestApi(
            this WebApplicationBuilder builder)
        {
            builder.Services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new Converters.NullableGuidJsonConverter());

                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter());
                });

            builder.Services.AddEndpointsApiExplorer();

            return builder;
        }
    }
}