using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Stock.Results;
using Inventory.Dto.Suppliers.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.State
{
    public class AppState
    {
        public List<ProductResult>? Products { get; set; }
        public List<ProductCatalogResult>? ProductsCatalog { get; set; }

        public List<CustomerResult>? Customers { get; set; }
        public List<SupplierResult>? Suppliers { get; set; }

        public Dictionary<Guid, StockResult>? StockMap { get; set; }
        public Dictionary<Guid, CustomerResult>? CustomerMap { get; set; }
        public Dictionary<Guid, SupplierResult>? SupplierMap { get; set; }

        public Dictionary<Guid, ProductResult>? ProductMap { get; set; }
        public Dictionary<Guid, ProductCatalogResult>? ProductCatalogMap { get; set; }
        public Dictionary<string, ProductResult>? BarcodeMap { get; set; }
        public Dictionary<string, ProductCatalogResult>? BarcodeCatalogMap { get; set; }

        public CashSessionResult? ActiveCashSession { get; set; }

        public bool IsPosLoaded { get; set; }
        public bool IsCashSessionLoaded { get; set; }

        public DateTime? LastBootstrapAt { get; set; }
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public bool IsCacheStale =>
            !IsPosLoaded ||
            LastBootstrapAt == null ||
            DateTime.UtcNow - LastBootstrapAt > CacheDuration;

        public void InvalidatePos()
        {
            IsPosLoaded = false;
            LastBootstrapAt = null;
        }

        // Mise à jour optimiste du stock
        public void DeductStock(Guid productId, decimal quantity)
        {
            if (StockMap == null || !StockMap.TryGetValue(productId, out var stock))
                return;

            stock.Quantity = Math.Max(0, stock.Quantity - quantity);
        }
    }
}
