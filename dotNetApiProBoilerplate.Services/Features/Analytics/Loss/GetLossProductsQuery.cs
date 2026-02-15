using Inventory.Dto.Analytics.Results;
using MediatR;

namespace Inventory.Services.Features.Analytics.Loss
{
    public record GetLossProductsQuery(DateOnly? From, DateOnly? To, int Limit = 10) : IRequest<LossProductsResponse>;


}
