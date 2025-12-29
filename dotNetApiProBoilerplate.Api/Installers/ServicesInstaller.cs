using Inventory.Infrastructure.Identity;
using Inventory.Infrastructure.Repositories;
using Inventory.Services;
using Inventory.Services.Behaviors;
using MediatR;

namespace Inventory.Api.Installers
{
    public static class ServicesInstaller
    {
        public static WebApplicationBuilder InstallServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<JwtTokenGenerator>();

            builder.Services.AddScoped<CustomerService>();
            builder.Services.AddScoped<CustomerTransactionService>();

            builder.Services.AddScoped<CashCorrectionService>();
            builder.Services.AddScoped<CashMovementService>();
            builder.Services.AddScoped<CashReportService>();
            builder.Services.AddScoped<CashSessionService>();

            builder.Services.AddScoped<PaymentService>();

            builder.Services.AddScoped<ProductService>();
            builder.Services.AddScoped<ProductCatalogService>();

            builder.Services.AddScoped<PurchaseService>();
            builder.Services.AddScoped<PurchaseLineService>();
            builder.Services.AddScoped<PurchasePaymentService>();

            builder.Services.AddScoped<ReturnService>();
            builder.Services.AddScoped<ReturnLineService>();


            builder.Services.AddScoped<SaleService>();
            builder.Services.AddScoped<SaleLineService>();

            builder.Services.AddScoped<StockService>();
            builder.Services.AddScoped<StockMouvementService>();

            builder.Services.AddScoped<SupplierService>();
            return builder;
        }
    }
}
