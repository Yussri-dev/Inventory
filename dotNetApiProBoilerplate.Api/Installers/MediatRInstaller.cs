using Inventory.Services;

namespace Inventory.Api.Installers
{
    public static class MediatRInstaller
    {
        public static WebApplicationBuilder InstallMediatR(this WebApplicationBuilder builder)
        {

            //builder.Services.AddMediatR(cfg =>
            //    cfg.RegisterServicesFromAssembly(typeof(PurchaseLineService).Assembly)
            //);

            builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(
                typeof(ProductService).Assembly,
                typeof(CustomerService).Assembly,
                typeof(SupplierService).Assembly,
                typeof(CashCorrectionService).Assembly,
                typeof(CashMovementService).Assembly,
                typeof(CashReportService).Assembly,
                typeof(CashSessionService).Assembly,
                typeof(PaymentService).Assembly,
                typeof(ProductCatalogService).Assembly,
                typeof(PurchaseService).Assembly,
                typeof(PurchaseLineService).Assembly,
                typeof(ReturnService).Assembly,
                typeof(ReturnLineService).Assembly,
                typeof(SaleService).Assembly,
                typeof(SaleLineService).Assembly,
                typeof(StockService).Assembly,
                typeof(StockMouvementService).Assembly
                )
            );
            return builder;
        }
    }
}
