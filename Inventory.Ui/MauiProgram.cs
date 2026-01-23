using Inventory.Ui;
using Inventory.Ui.Authentification;
using Inventory.Ui.Infrastructure;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Refit;

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


        // =========================
        // CORE SERVICES
        // =========================
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<JwtAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();

        builder.Services.AddTransient<AuthHeaderHandler>();
        builder.Services.AddTransient<AuthExpiredHandler>();
        builder.Services.AddTransient<RefreshTokenHandler>();

        // =========================
        // OPEN API (NO AUTH)
        // =========================
        builder.Services.AddOpenApi<IAuthApiOpen>(apiBaseUrl);

        // =========================
        // SECURED APIs (JWT + REFRESH)
        // =========================
        builder.Services.AddSecuredApi<IAuthApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<ICashSessionApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<IProductApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<IProductCatalogApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<ISupplierApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<ICustomerApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<IPurchaseApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<ISaleApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<IInventorySessionApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<IInventoryLineApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<IStockApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<IStockMovementApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<IReturnApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<IReturnLineApi>(apiBaseUrl);
        builder.Services.AddSecuredApi<ICustomerTransactionsApi>(apiBaseUrl);


        return builder.Build();
    }
}
