using Inventory.Dto.Analytics.Results;
using MediatR;


namespace Inventory.Services.Features.Analytics.Dashboard
{
    public record GetDashboardSummaryQuery(
        DateOnly? From,
        DateOnly? To
    ) : IRequest<DashboardSummaryResult>;
}
