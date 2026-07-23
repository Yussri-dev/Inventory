using Inventory.Dto.Analytics.Results;
using Inventory.LocalDB.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Reflection;

namespace Inventory.Ui.Services.Analytics
{
    public class LocalAnalyticsService : ILocalAnalyticsService
    {
        private readonly PosLocalDbContext _db;

        public LocalAnalyticsService(PosLocalDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardSummaryResult> GetDashboardSummaryAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            var fromDate = from.ToDateTime(TimeOnly.MinValue);
            var toDateExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var sales = await _db.Sales
                .AsNoTracking()
                .Include(x => x.Lines)
                .Include(x => x.Payments)
                .Where(x =>
                    x.SaleDateUtc >= fromDate &&
                    x.SaleDateUtc < toDateExclusive)
                .OrderByDescending(x => x.SaleDateUtc)
                .ToListAsync(cancellationToken);

            var dashboard = new DashboardSummaryResult();

            var revenue = sales.Sum(x => GetDecimal(x, "TotalAmount"));
            var refunds = 0m;
            var cost = 0m;
            var profit = revenue - refunds - cost;
            var salesCount = sales.Count;
            var averageBasket = salesCount > 0 ? revenue / salesCount : 0m;

            var cashRevenue = sales
                .SelectMany(x => x.Payments)
                .Where(x => IsPaymentMethod(x, "Cash"))
                .Sum(x => GetDecimal(x, "Amount"));

            var cardRevenue = sales
                .SelectMany(x => x.Payments)
                .Where(x => IsPaymentMethod(x, "Card"))
                .Sum(x => GetDecimal(x, "Amount"));

            var creditRevenue = sales
                .SelectMany(x => x.Payments)
                .Where(x => IsPaymentMethod(x, "Credit"))
                .Sum(x => GetDecimal(x, "Amount"));

            Set(dashboard, "Revenue", revenue);
            Set(dashboard, "Refunds", refunds);
            Set(dashboard, "Cost", cost);
            Set(dashboard, "Profit", profit);
            Set(dashboard, "Margin", revenue > 0 ? profit / revenue * 100m : 0m);
            Set(dashboard, "CreditRevenue", creditRevenue);
            Set(dashboard, "SalesCount", salesCount);
            Set(dashboard, "AverageBasket", averageBasket);
            Set(dashboard, "CashRevenue", cashRevenue);
            Set(dashboard, "CardRevenue", cardRevenue);
            Set(dashboard, "LossRate", 0m);
            Set(dashboard, "TotalLoss", 0m);

            FillRecentSales(dashboard, sales);
            FillTopProducts(dashboard, sales);

            return dashboard;
        }

        public Task<List<LossProductResult>> GetLossProductsAsync(
            DateOnly from,
            DateOnly to,
            int take = 10,
            CancellationToken cancellationToken = default)
        {
            // For now local loss/returns are not implemented yet.
            // Later we will calculate this from LocalReturn and LocalReturnLine.
            return Task.FromResult(new List<LossProductResult>());
        }

        public async Task<WeeklyReportResult> GetWeeklyAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            var dashboard = await GetDashboardSummaryAsync(from, to, cancellationToken);

            var weekly = new WeeklyReportResult();

            Set(weekly, "Week", $"{from:dd/MM} - {to:dd/MM}");
            Set(weekly, "Revenue", GetDecimal(dashboard, "Revenue"));
            Set(weekly, "Expenses", GetDecimal(dashboard, "Cost"));
            Set(weekly, "Profit", GetDecimal(dashboard, "Profit"));
            Set(weekly, "SalesCount", GetInt(dashboard, "SalesCount"));
            Set(weekly, "ReturnsCount", 0);

            return weekly;
        }

        private static void FillRecentSales(
            DashboardSummaryResult dashboard,
            IEnumerable<object> sales)
        {
            var recentSalesProperty = dashboard.GetType().GetProperty("RecentSales");

            if (recentSalesProperty == null)
                return;

            var list = recentSalesProperty.GetValue(dashboard) as IList;

            if (list == null)
            {
                list = CreateListForProperty(recentSalesProperty);
                recentSalesProperty.SetValue(dashboard, list);
            }

            list.Clear();

            var itemType = GetListItemType(recentSalesProperty);

            if (itemType == null)
                return;

            foreach (var sale in sales.Take(10))
            {
                var item = Activator.CreateInstance(itemType);

                if (item == null)
                    continue;

                Set(item, "InvoiceNumber", GetString(sale, "LocalInvoiceNumber"));
                Set(item, "SaleDate", GetDateTime(sale, "SaleDateUtc"));
                Set(item, "CustomerName", GetString(sale, "CustomerName") ?? "Walk-in");
                Set(item, "PaymentSummary", BuildPaymentSummary(sale));
                Set(item, "TotalAmount", GetDecimal(sale, "TotalAmount"));

                list.Add(item);
            }
        }

        private static void FillTopProducts(
            DashboardSummaryResult dashboard,
            IEnumerable<object> sales)
        {
            var topProductsProperty = dashboard.GetType().GetProperty("TopProducts");

            if (topProductsProperty == null)
                return;

            var list = topProductsProperty.GetValue(dashboard) as IList;

            if (list == null)
            {
                list = CreateListForProperty(topProductsProperty);
                topProductsProperty.SetValue(dashboard, list);
            }

            list.Clear();

            var itemType = GetListItemType(topProductsProperty);

            if (itemType == null)
                return;

            var lines = sales
                .SelectMany(sale => GetEnumerable(sale, "Lines"))
                .GroupBy(line => GetString(line, "ProductName") ?? "Unknown product")
                .Select(group => new
                {
                    ProductName = group.Key,
                    QuantitySold = group.Sum(x => GetDecimal(x, "Quantity")),
                    TotalRevenue = group.Sum(x =>
                    {
                        var lineTotal = GetDecimal(x, "LineAmountInclVat");

                        if (lineTotal > 0)
                            return lineTotal;

                        var quantity = GetDecimal(x, "Quantity");
                        var unitPrice = GetDecimal(x, "UnitPrice");
                        var discount = GetDecimal(x, "DiscountAmount");

                        return Math.Max(0, quantity * unitPrice - discount);
                    })
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            foreach (var line in lines)
            {
                var item = Activator.CreateInstance(itemType);

                if (item == null)
                    continue;

                Set(item, "ProductName", line.ProductName);
                Set(item, "QuantitySold", line.QuantitySold);
                Set(item, "TotalRevenue", line.TotalRevenue);

                list.Add(item);
            }
        }

        private static string BuildPaymentSummary(object sale)
        {
            var payments = GetEnumerable(sale, "Payments").ToList();

            if (payments.Count == 0)
                return "Unpaid";

            return string.Join(
                " + ",
                payments
                    .Select(x => GetString(x, "Method") ?? GetString(x, "PaymentMethod") ?? "Payment")
                    .Distinct());
        }

        private static bool IsPaymentMethod(object payment, string method)
        {
            var value = GetString(payment, "Method")
                ?? GetString(payment, "PaymentMethod")
                ?? string.Empty;

            return value.Equals(method, StringComparison.OrdinalIgnoreCase);
        }

        private static IList CreateListForProperty(PropertyInfo property)
        {
            var itemType = GetListItemType(property) ?? typeof(object);
            var listType = typeof(List<>).MakeGenericType(itemType);

            return (IList)Activator.CreateInstance(listType)!;
        }

        private static Type? GetListItemType(PropertyInfo property)
        {
            if (property.PropertyType.IsGenericType)
            {
                return property.PropertyType.GetGenericArguments().FirstOrDefault();
            }

            return null;
        }

        private static IEnumerable<object> GetEnumerable(object source, string propertyName)
        {
            var value = source.GetType().GetProperty(propertyName)?.GetValue(source);

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                        yield return item;
                }
            }
        }

        private static string? GetString(object source, string propertyName)
        {
            var value = source.GetType().GetProperty(propertyName)?.GetValue(source);

            return value?.ToString();
        }

        private static decimal GetDecimal(object source, string propertyName)
        {
            var value = source.GetType().GetProperty(propertyName)?.GetValue(source);

            if (value == null)
                return 0m;

            if (value is decimal d)
                return d;

            if (value is int i)
                return i;

            if (value is double db)
                return Convert.ToDecimal(db);

            if (value is float f)
                return Convert.ToDecimal(f);

            return decimal.TryParse(value.ToString(), out var parsed)
                ? parsed
                : 0m;
        }

        private static int GetInt(object source, string propertyName)
        {
            var value = source.GetType().GetProperty(propertyName)?.GetValue(source);

            if (value == null)
                return 0;

            if (value is int i)
                return i;

            if (value is decimal d)
                return (int)d;

            return int.TryParse(value.ToString(), out var parsed)
                ? parsed
                : 0;
        }

        private static DateTime GetDateTime(object source, string propertyName)
        {
            var value = source.GetType().GetProperty(propertyName)?.GetValue(source);

            if (value == null)
                return DateTime.UtcNow;

            if (value is DateTime dt)
                return dt;

            return DateTime.TryParse(value.ToString(), out var parsed)
                ? parsed
                : DateTime.UtcNow;
        }

        private static void Set(object target, string propertyName, object? value)
        {
            var property = target.GetType().GetProperty(propertyName);

            if (property == null || !property.CanWrite)
                return;

            if (value == null)
            {
                property.SetValue(target, null);
                return;
            }

            var targetType = Nullable.GetUnderlyingType(property.PropertyType)
                ?? property.PropertyType;

            try
            {
                if (targetType.IsEnum)
                {
                    property.SetValue(target, Enum.Parse(targetType, value.ToString()!));
                    return;
                }

                var converted = Convert.ChangeType(value, targetType);
                property.SetValue(target, converted);
            }
            catch
            {
                // Ignore property mismatch to keep local analytics robust.
            }
        }
    }
}
