namespace Inventory.Dto.Tenants.Results
{
    public class TenantStatsResponse
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalSalesThisMonth { get; set; }
        public decimal TotalRevenueThisMonth { get; set; }
    }
}
