// Import the services layer
// Used here only to reference an assembly that contains AutoMapper profiles
using Inventory.Services;

namespace Inventory.Api.Installers
{
    // Static installer class following the Installer pattern
    // Purpose: centralize AutoMapper configuration
    public static class MappingInstaller
    {
        // Extension method used in Program.cs
        // Registers AutoMapper and scans assemblies for mapping profiles
        public static WebApplicationBuilder InstallMapping(this WebApplicationBuilder builder)
        {
            builder.Services.AddAutoMapper(typeof(ProductService).Assembly);
            builder.Services.AddAutoMapper(typeof(CustomerService).Assembly);
            builder.Services.AddAutoMapper(typeof(CashCorrectionService).Assembly);
            builder.Services.AddAutoMapper(typeof(CashMovementService).Assembly);

            return builder;
        }
    }
}
