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


            builder.Services.AddScoped<ProductService>();
            builder.Services.AddScoped<CustomerService>();
            builder.Services.AddScoped<CashCorrectionService>();


            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<JwtTokenGenerator>();

            return builder;
        }
    }
}
