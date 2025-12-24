using Inventory.Infrastructure.Repositories;
using MediatR;

namespace Inventory.Services.Behaviors
{
    public class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly IUnitOfWork _uow;

        public UnitOfWorkBehavior(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = await next();
            await _uow.SaveChangesAsync();
            return response;
        }
    }
}
