using Inventory.Dto.Analytics.Results;

namespace Inventory.Ui.Services.Analytics
{
    public interface ILocalAnalyticsService
    {
        Task<DashboardSummaryResult> GetDashboardSummaryAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default);

        Task<List<LossProductResult>> GetLossProductsAsync(
            DateOnly from,
            DateOnly to,
            int take = 10,
            CancellationToken cancellationToken = default);

        Task<WeeklyReportResult> GetWeeklyAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default);
    }
}
