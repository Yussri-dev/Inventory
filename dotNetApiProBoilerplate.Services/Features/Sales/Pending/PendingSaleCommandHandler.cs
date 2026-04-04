using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.Pending
{
    public class PendingSaleCommandHandler : IRequestHandler<PendingSaleCommand, SaleResult>
    {
        private readonly SaleService _saleService;
        public PendingSaleCommandHandler(SaleService saleService)
        {
            _saleService = saleService;
        }

        public Task<SaleResult> Handle(PendingSaleCommand command, CancellationToken ct)
        {
            return _saleService.CreatePendingAsync(command.Request);
        }
    }
}
