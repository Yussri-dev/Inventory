using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Installers
{
    public static class DatabaseInstaller
    {
        static string GetConnectionString(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString)) return connectionString;

            connectionString = configuration["ConnectionStrings__DefaultConnection"];
            if (!string.IsNullOrEmpty(connectionString)) return connectionString;

            var databaseUrl = configuration["DATABASE_URL"];
            if (!string.IsNullOrEmpty(databaseUrl)) return ConvertDatabaseUrl(databaseUrl);

            throw new InvalidOperationException("No PostgreSQL connection string found (set ConnectionStrings__DefaultConnection or DATABASE_URL).");
        }


        static string ConvertDatabaseUrl(string databaseUrl)
        {
            if (string.IsNullOrWhiteSpace(databaseUrl))
                throw new ArgumentException("DATABASE_URL cannot be null or empty", nameof(databaseUrl));

            var normalizedUrl = databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                && !databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                ? databaseUrl.Replace("postgres://", "postgresql://", StringComparison.OrdinalIgnoreCase)
                : databaseUrl;

            if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException($"Invalid DATABASE_URL format: {databaseUrl}", nameof(databaseUrl));

            if (string.IsNullOrWhiteSpace(uri.Host)) throw new ArgumentException("DATABASE_URL must contain a valid host");
            if (string.IsNullOrWhiteSpace(uri.UserInfo)) throw new ArgumentException("DATABASE_URL must contain user credentials");

            var userInfo = uri.UserInfo.Split(':');
            if (userInfo.Length != 2) throw new ArgumentException("DATABASE_URL must contain both username and password");

            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = Uri.UnescapeDataString(userInfo[1]);
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = string.IsNullOrWhiteSpace(uri.LocalPath) || uri.LocalPath == "/"
                ? throw new ArgumentException("DATABASE_URL must contain a database name")
                : uri.LocalPath.Substring(1);

            return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        }


        public static WebApplicationBuilder InstallDatabase(this WebApplicationBuilder builder)
        {
            var connectionString = GetConnectionString(builder.Configuration);

            //builder.Services.AddDbContext<InventoryDbContext>(options =>
            //    options.UseInMemoryDatabase("BoilerplateDb"));

            builder.Services.AddDbContext<InventoryDbContext>(opt =>

                opt.UseNpgsql(connectionString)
            );
            return builder;
        }
    }
}
