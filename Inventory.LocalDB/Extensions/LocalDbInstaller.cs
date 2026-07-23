using Inventory.LocalDB.Context;
using Inventory.LocalDB.Services;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.LocalDB.Extensions
{
    public static class LocalDbInstaller
    {
        public static IServiceCollection AddLocalDb(this IServiceCollection services, string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentException("Local database path is required.", nameof(dbPath));

            services.AddDbContext<PosLocalDbContext>(options =>
            {
                options.UseSqlite($"Data Source={dbPath}");
            });

            services.AddScoped<ILocalDatabaseInitializer, LocalDatabaseInitializer>();

            services.AddScoped<ILocalUserSessionService, LocalUserSessionService>();
            services.AddScoped<ILocalProductService, LocalProductService>();
            services.AddScoped<ILocalCashSessionService, LocalCashSessionService>();
            services.AddScoped<ILocalSaleService, LocalSaleService>();
            services.AddScoped<ILocalCustomerService, LocalCustomerService>();
            services.AddScoped<ILocalSupplierService, LocalSupplierService>();
            services.AddScoped<ISyncQueueService, SyncQueueService>();

            return services;
        }
    }
}