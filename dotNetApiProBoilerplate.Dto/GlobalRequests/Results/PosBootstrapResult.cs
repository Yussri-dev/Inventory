using Inventory.Dto.Customers.Results;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Stock.Results;
using Inventory.Dto.Suppliers.Results;


namespace Inventory.Dto.GlobalRequests.Results
{
    public class PosBootstrapResult
    {
        public DateTime ServerTime { get; set; }
        public PosConfigResult Config { get; set; } = new();

        public List<ProductResult> Products { get; set; }
        public List<StockResult> Stocks { get; set; }
        public List<CustomerResult> Customers { get; set; }
        public List<SupplierResult> Suppliers { get; set; }
        public List<ProductCatalogResult> ProductCatalogs { get; set; }
    }

}
