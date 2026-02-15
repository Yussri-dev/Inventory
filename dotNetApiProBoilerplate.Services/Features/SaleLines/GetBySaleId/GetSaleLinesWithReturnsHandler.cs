using Inventory.Domain.Entities;
using Inventory.Dto.SaleLines.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;
using MediatR;


namespace Inventory.Services.Features.SaleLines.GetBySaleId
{
    public class GetSaleLinesWithReturnsHandler
    : IRequestHandler<GetSaleLinesWithReturnsQuery, List<SaleLineWithReturnsResult>>
    {
        private readonly IRepository<SaleLine> _saleLineRepo;
        private readonly IRepository<ReturnLine> _returnLineRepo;
        private readonly IRepository<Return> _returnRepo;
        private readonly IRepository<Sale> _saleRepo;
        private readonly ITenantContext _tenantContext;

        public GetSaleLinesWithReturnsHandler(
            IRepository<SaleLine> saleLineRepo,
            IRepository<ReturnLine> returnLineRepo,
            IRepository<Return> returnRepo,
            IRepository<Sale> saleRepo,
            ITenantContext tenantContext)
        {
            _saleLineRepo = saleLineRepo;
            _returnLineRepo = returnLineRepo;
            _saleRepo = saleRepo;
            _tenantContext = tenantContext;
            _returnRepo = returnRepo;
        }

        public async Task<List<SaleLineWithReturnsResult>> Handle(
     GetSaleLinesWithReturnsQuery request,
     CancellationToken cancellationToken)
        {
            var tenantId = _tenantContext.TenantId;

            var sale = await _saleRepo.GetByIdAsync(request.SaleId);
            if (sale == null || sale.IsDeleted || sale.TenantId != tenantId)
                throw new NotFoundException("Sale", request.SaleId);

            var saleLines = (await _saleLineRepo.GetAllAsync())
                .Where(sl => sl.SaleId == request.SaleId)
                .ToList();

            var returns = await _returnRepo.GetAllAsync();
            var returnLines = await _returnLineRepo.GetAllAsync();

            var saleReturnIds = returns
                .Where(r => r.SaleId == request.SaleId && !r.IsDeleted)
                .Select(r => r.Id)
                .ToHashSet();

            var result = saleLines.Select(sl =>
            {
                var returnedQty = returnLines
                    .Where(rl =>
                        rl.ProductId == sl.ProductId &&
                        saleReturnIds.Contains(rl.ReturnId) &&
                        !rl.IsDeleted)
                    .Sum(rl => rl.Quantity);

                var available = sl.Quantity - returnedQty;
                if (available < 0) available = 0;

                return new SaleLineWithReturnsResult
                {
                    Id = sl.Id,
                    SaleId = sl.SaleId,
                    ProductId = sl.ProductId,
                    Quantity = sl.Quantity,
                    ReturnedQuantity = returnedQty,
                    AvailableQuantity = available,
                    UnitPrice = sl.UnitPrice,
                    VatRate = sl.VatRate,
                    LineAmountInclVat = sl.Quantity * sl.UnitPrice
                };
            }).ToList();

            return result;
        }

    }

}
