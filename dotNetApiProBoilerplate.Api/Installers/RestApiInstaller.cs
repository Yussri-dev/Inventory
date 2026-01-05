namespace Inventory.Api.Installers
{
    public static class RestApiInstaller
    {
        public static WebApplicationBuilder InstallRestApi(this WebApplicationBuilder builder)
        {
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new Inventory.Api.Converters.NullableGuidJsonConverter());
                });

            builder.Services.AddEndpointsApiExplorer();

            return builder;
        }
    }
}
