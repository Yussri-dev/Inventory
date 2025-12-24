namespace Inventory.Api.Installers
{
    public static class RestApiInstaller
    {
        public static WebApplicationBuilder InstallRestApi(this WebApplicationBuilder builder)
        {
            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();

            return builder;
        }
    }
}
