using Inventory.Dto.Analytics.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IAnalyticsApi
    {
        [Get("/api/v1/analytics/profit")]
        Task<ProfitAnalyticsResult> GetProfitAsync(
            [Query] DateOnly? from = null,
            [Query] DateOnly? to = null);

        [Get("/api/v1/analytics/loss-products")]
        Task<LossProductsResponse> GetLossProductsAsync(
            [Query] DateOnly? from = null,
            [Query] DateOnly? to = null,
            [Query] int limit = 10);

        [Get("/api/v1/analytics/weekly")]
        Task<WeeklyReportResult> GetWeeklyAsync(
            [Query] int? year = null,
            [Query] int? week = null);

        [Get("/api/v1/analytics/dashboard-summary")]
        Task<DashboardSummaryResult> GetDashboardSummaryAsync(
            [Query] DateOnly? from = null,
            [Query] DateOnly? to = null);

    }
}
