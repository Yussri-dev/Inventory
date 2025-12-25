using Inventory.Services;

namespace Inventory.Api.Installers
{
    public static class MediatRInstaller
    {
        public static WebApplicationBuilder InstallMediatR(this WebApplicationBuilder builder)
        {
            // Scan the Services assembly (where handlers will live)
            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(ProductService).Assembly)
            );

            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(CustomerService).Assembly)
            );

            builder.Services.AddMediatR(cfg =>
               cfg.RegisterServicesFromAssembly(typeof(CashCorrectionService).Assembly)
            );

            builder.Services.AddMediatR(cfg =>
               cfg.RegisterServicesFromAssembly(typeof(CashMovementService).Assembly)
            );

            builder.Services.AddMediatR(cfg =>
               cfg.RegisterServicesFromAssembly(typeof(CashReportService).Assembly)
            );

            builder.Services.AddMediatR(cfg =>
              cfg.RegisterServicesFromAssembly(typeof(CashSessionService).Assembly)
            );

            builder.Services.AddMediatR(cfg =>
                 cfg.RegisterServicesFromAssembly(typeof(PaymentService).Assembly)
            );
            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(ProductCatalogService).Assembly)
             );

            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(PurchaseService).Assembly)
             );
            return builder;
        }
    }
}
