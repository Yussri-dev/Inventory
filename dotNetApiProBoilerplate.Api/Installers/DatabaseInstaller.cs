using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Installers
{
    public static class DatabaseInstaller
    {
        public static WebApplicationBuilder InstallDatabase(this WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<InventoryDbContext>(options =>
                options.UseInMemoryDatabase("BoilerplateDb"));

            return builder;
        }
    }
}
