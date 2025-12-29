using Inventory.Dto.Returns.Results;
using MediatR;

namespace Inventory.Services.Features.Returns.GetById
{
    public class GetReturnByIdQueryHandler
        : IRequestHandler<GetReturnByIdQuery, ReturnResult>
    {
        private readonly ReturnService _productService;

        public GetReturnByIdQueryHandler(ReturnService productService)
        {
            _productService = productService;
        }

        public Task<ReturnResult> Handle(GetReturnByIdQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetByIdAsync(query.Id);
        }
    }
}
