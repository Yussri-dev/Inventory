using Inventory.LocalDB.Extensions;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui;
using Inventory.Ui.Authentification;
using Inventory.Ui.Infrastructure;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services;
using Inventory.Ui.Services.Analytics;
using Inventory.Ui.Services.Sync;
using Inventory.Ui.State;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using QuestPDF.Infrastructure;
using System.Text;

//#if WINDOWS
//using Inventory.Ui.Platforms.Windows.Printing;
//#endif

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        QuestPDF.Settings.License = LicenseType.Community;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont(
                    "OpenSans-Regular.ttf",
                    "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

#if ANDROID
        const string apiBaseUrl = "https://10.0.2.2:7190";
#else
        const string apiBaseUrl = "https://localhost:7190";
#endif

        // =========================
        // LOCAL OFFLINE DATABASE
        // =========================

        var localDbPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "inventory-pos.db");

        builder.Services.AddLocalDb(localDbPath);

        // =========================
        // AUTHENTICATION
        // =========================

        builder.Services.AddAuthorizationCore();

        builder.Services.AddScoped<JwtAuthStateProvider>();

        builder.Services.AddScoped<
            AuthenticationStateProvider,
            JwtAuthStateProvider>();

        builder.Services.AddSingleton<
            ISecureStorageService,
            SecureStorageService>();

        builder.Services.AddTransient<AuthHeaderHandler>();
        builder.Services.AddTransient<AuthExpiredHandler>();
        builder.Services.AddTransient<RefreshTokenHandler>();

        // =========================
        // OPEN API
        // =========================

        builder.Services.AddOpenApi<IAuthApiOpen>(
            apiBaseUrl);

        // =========================
        // LOCAL SERVICES
        // =========================

        builder.Services.AddScoped<
            ILocalSyncUploader,
            LocalSyncUploader>();

        builder.Services.AddScoped<
            ILocalAnalyticsService,
            LocalAnalyticsService>();

        builder.Services.AddScoped<
            ILocalProductCatalogQueryService,
            LocalProductCatalogQueryService>();

        builder.Services.AddScoped<
            ILocalProductCategoryQueryService,
            LocalProductCategoryQueryService>();

        builder.Services.AddScoped<
            ILocalProductCategorySyncService,
            LocalProductCategorySyncService>();

        builder.Services.AddScoped<
            ILocalProductCatalogSyncService,
            LocalProductCatalogSyncService>();

        builder.Services.AddSingleton<
            ILocalTenantContext,
            LocalTenantContext>();

        builder.Services.AddScoped<
            ILocalProductSyncService,
            LocalProductSyncService>();

        builder.Services.AddScoped<
            ILocalStockSyncService,
            LocalStockSyncService>();

        builder.Services.AddScoped<
            ILocalCustomerService,
            LocalCustomerService>();

        builder.Services.AddScoped<
            ILocalCustomerSyncService,
            LocalCustomerSyncService>();

        builder.Services.AddScoped<
           LocalDataBootstrapService>();

        builder.Services.AddScoped<
            ILocalSupplierService,
            LocalSupplierService>();

        builder.Services.AddScoped<
            ILocalSupplierSyncService,
            LocalSupplierSyncService>();

        builder.Services.AddScoped<
            ILocalDamageService,
            LocalDamageService>();

        builder.Services.AddScoped<
            ILocalDamageSyncService,
            LocalDamageSyncService>();

        builder.Services.AddScoped<
            ILocalPurchaseService,
            LocalPurchaseService>();

        // Local SQLite stock query and manual adjustment service.
        builder.Services.AddScoped<
            ILocalStockAdjustmentService,
            LocalStockAdjustmentService>();

        // Uploads only manual StockMovement outbox items.
        // Sale/Purchase/Return stock movements are synchronized by their
        // authoritative complete endpoints and must not be uploaded twice.
        builder.Services.AddScoped<
            ILocalStockMovementUploadService,
            LocalStockMovementUploadService>();

        // Pulls server stock after pushing manual adjustments.
        // Replace any existing ILocalStockSyncService registration with this one.
        builder.Services.AddScoped<
            ILocalStockSyncService,
            LocalStockSyncService>();

        // Inventory.LocalDB services
        builder.Services.AddScoped<
            ILocalReturnService,
            LocalReturnService>();

        // Inventory.Ui sync services
        builder.Services.AddScoped<
            ILocalReturnUploadService,
            LocalReturnUploadService>();

        builder.Services.AddScoped<
           ILocalCustomerService,
           LocalCustomerService>();

        builder.Services.AddScoped<
          ILocalCustomerCreditService,
          LocalCustomerCreditService>();

        builder.Services.AddScoped<
          ILocalCustomerTransactionUploadService,
          LocalCustomerTransactionUploadService>();

        builder.Services.AddScoped<
           ILocalCustomerSyncService,
           LocalCustomerSyncService>();

        builder.Services.AddScoped<
            ICashSessionReconciliationService,
            CashSessionReconciliationService>();

        builder.Services.AddScoped<
            ILocalSalesHistoryService,
            LocalSalesHistoryService>();

        builder.Services.Configure<ReceiptSettings>(
     settings =>
     {
         settings.CompanyName =
             "My Store";

         settings.CompanyAddress =
             "Kortrijk, Belgium";

         settings.CompanyPhone =
             "+32 ...";

         settings.CompanyEmail =
             "contact@example.com";

         settings.CompanyTaxNumber =
             "BE0123.456.789";

         settings.DefaultCashierName =
             "POS";

         settings.FooterText =
             "Thank you for your purchase.";
     });

        builder.Services.Configure<ReceiptPrinterOptions>(
            options =>
            {
                /*
                 * Nom exact visible dans les paramètres des imprimantes
                 * Windows.
                 */
                options.PrinterName =
                    "EPSON TM-T20III Receipt";

                /*
                 * 80 mm : 42 ou 48 caractères.
                 * 58 mm : généralement 32 caractères.
                 */
                options.CharactersPerLine =
                    48;

                options.CodePage =
                    858;

                options.CutPaper =
                    true;

                options.FeedLinesAfterReceipt =
                    4;

                options.ReceiptTitle =
                    "TICKET DE CAISSE";
            });

        builder.Services.AddScoped<
            IReceiptService,
            ReceiptService>();

        builder.Services.AddScoped<
            IReceiptPdfGenerator,
            ReceiptPdfGenerator>();

        builder.Services.AddScoped<
            IReceiptPrinter,
            ReceiptPrinter>();

        #if WINDOWS
        builder.Services.AddSingleton<
            IReceiptPrinterTransport,
            WindowsRawPrinterTransport>();
        #endif

        // AutoSyncService may remain singleton only if it creates
        // its own IServiceScope for scoped synchronization services.
        builder.Services.AddSingleton<
            IAutoSyncService,
            AutoSyncService>();

        // =========================
        // STATES
        // =========================

        builder.Services.AddSingleton<AppState>();
        builder.Services.AddSingleton<PosBootstrapService>();
        builder.Services.AddSingleton<PosState>();

        // =========================
        // SECURED APIs
        // =========================

        builder.Services.AddSecuredApi<IAuthApi>(
            apiBaseUrl,
            withRefresh: false);

        builder.Services.AddSecuredApi<ICashSessionApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IProductApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IProductCatalogApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IProductCategoryApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IAdminProductApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<ISupplierApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<ICustomerApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IPurchaseApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<ISaleApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<ISaleLineApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IInventorySessionApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IInventoryLineApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IStockApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IStockMovementApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IReturnApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IReturnLineApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<ICustomerTransactionsApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IAnalyticsApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IDamageApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<IPosApi>(
            apiBaseUrl);

        builder.Services.AddSecuredApi<ILoyaltyCardsApi>(
            apiBaseUrl);

        builder.Services.AddMudServices();

        var app = builder.Build();

        // =========================
        // INITIALIZE LOCAL DATABASE
        // =========================

        using (var scope = app.Services.CreateScope())
        {
            var initializer = scope.ServiceProvider
                .GetRequiredService<ILocalDatabaseInitializer>();

            initializer
                .InitializeAsync()
                .GetAwaiter()
                .GetResult();
        }

        // Ne pas démarrer AutoSync ici.
        // À ce moment, aucun JWT valide n'est forcément disponible.

        return app;
    }
}