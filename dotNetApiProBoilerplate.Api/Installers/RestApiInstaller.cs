using System.Text.Json.Serialization;

namespace Inventory.Api.Installers
{
    public static class RestApiInstaller
    {
        public static WebApplicationBuilder InstallRestApi(this WebApplicationBuilder builder)
        {
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new Converters.NullableGuidJsonConverter());
               
                    var enumConverter = options.JsonSerializerOptions.Converters.FirstOrDefault(
                        c => c is JsonStringEnumConverter);

                    if (enumConverter != null)
                    {
                        options.JsonSerializerOptions.Converters.Remove(enumConverter);
                    }
                
                });

            builder.Services.AddEndpointsApiExplorer();

            return builder;
        }
    }
}
