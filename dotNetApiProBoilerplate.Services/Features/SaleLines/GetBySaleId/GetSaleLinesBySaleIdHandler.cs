using Inventory.Dto.SaleLines.Results;
using MediatR;


namespace Inventory.Services.Features.SaleLines.GetBySaleId
{
    public class GetSaleLinesBySaleIdHandler
    : IRequestHandler<GetSaleLinesBySaleIdQuery, List<SaleLineResult>>
    {
        private readonly SaleLineService _service;

        public GetSaleLinesBySaleIdHandler(SaleLineService service)
        {
            _service = service;
        }

        public Task<List<SaleLineResult>> Handle(
            GetSaleLinesBySaleIdQuery request,
            CancellationToken cancellationToken)
        {
            return _service.GetBySaleIdAsync(request.SaleId);
        }
    }

}
