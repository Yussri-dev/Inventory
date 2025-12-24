using Inventory.Domain.Models;
using Inventory.Infrastructure.Data;

using Inventory.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;

public static class IdentityInstaller
{
    public static WebApplicationBuilder InstallIdentity(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;

            options.Password.RequiredLength = 8;

            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<InventoryDbContext>()

        .AddDefaultTokenProviders();

        return builder;
    }
}
