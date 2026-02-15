using Inventory.Dto.Analytics.Results;
using MediatR;

namespace Inventory.Services.Features.Analytics.WeeklyReport
{
    public record GetWeeklyReportQuery(int? Year, int? Week) : IRequest<WeeklyReportResult>;
}
