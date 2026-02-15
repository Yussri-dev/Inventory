using Inventory.Dto.Analytics.Results;
using MediatR;

namespace Inventory.Services.Features.Analytics.Profit
{
    public record GetProfitAnalyticsQuery(DateOnly? From, DateOnly? To) : IRequest<ProfitAnalyticsResult>;

}
