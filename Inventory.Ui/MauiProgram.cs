using Inventory.Ui.Infrastructure;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            #if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
            #endif

            #if ANDROID
                var apiBaseUrl = "https://10.0.2.2:7190";
            #else
                var apiBaseUrl = "https://localhost:7190";
            #endif

            builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
            builder.Services.AddTransient<AuthHeaderHandler>();

            builder.Services.AddOpenApi<IAuthApiOpen>(apiBaseUrl);
            builder.Services.AddSecuredApi<IAuthApi>(apiBaseUrl);
            builder.Services.AddSecuredApi<IProductCatalogApi>(apiBaseUrl);
            builder.Services.AddSecuredApi<ISupplierApi>(apiBaseUrl);

            return builder.Build();
        }
    }
}
