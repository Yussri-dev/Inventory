using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Analytics.Results
{
    public class DashboardSummaryResult
    {
        public decimal Revenue { get; set; }
        public decimal Refunds { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public decimal Margin { get; set; }

        public int SalesCount { get; set; }
        public decimal AverageBasket { get; set; }
        
        public decimal CashRevenue { get; set; }
        public decimal CardRevenue { get; set; }
        public decimal CreditRevenue { get; set; }

        public decimal TotalLoss { get; set; }
        public decimal LossRate { get; set; }

        public List<RecentSaleResult> RecentSales { get; set; } = new List<RecentSaleResult>();
        public List<TopProductResult> TopProducts { get; set; } = new List<TopProductResult>();
    }
}
