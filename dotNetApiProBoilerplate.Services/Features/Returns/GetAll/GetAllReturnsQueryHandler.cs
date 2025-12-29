using Inventory.Dto.Returns.Results;
using MediatR;

namespace Inventory.Services.Features.Returns.GetAll
{
    public class GetAllReturnsQueryHandler
        : IRequestHandler<GetAllReturnsQuery, List<ReturnResult>>
    {
        private readonly ReturnService _productService;

        public GetAllReturnsQueryHandler(ReturnService productService)
        {
            _productService = productService;
        }

        public Task<List<ReturnResult>> Handle(GetAllReturnsQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetAllAsync();
        }
    }
}
