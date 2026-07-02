using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.ProductCategory.Results;
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
        // ── Change notification ──────────────────────────────────────────────────
        // Components do: protected override void OnInitialized() => State.OnChange += StateHasChanged;
        // and dispose:   State.OnChange -= StateHasChanged;
        public event Action? OnChange;

        // ── Internal lock ────────────────────────────────────────────────────────
        private readonly SemaphoreSlim _lock = new(1, 1);

        // ── Cache duration ───────────────────────────────────────────────────────
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        // ── Backing fields (never write directly from outside) ───────────────────
        private List<ProductResult>? _products;
        private List<ProductCatalogResult>? _productsCatalog;
        private List<ProductCategoryResult>? _productsCategory;
        private List<CustomerResult>? _customers;
        private List<SupplierResult>? _suppliers;
        private Dictionary<Guid, StockResult>? _stockMap;
        private Dictionary<Guid, CustomerResult>? _customerMap;
        private Dictionary<Guid, SupplierResult>? _supplierMap;
        private Dictionary<Guid, ProductResult>? _productMap;
        private Dictionary<Guid, ProductCatalogResult>? _productCatalogMap;
        private Dictionary<Guid, ProductCategoryResult>? _productCategoryMap;
        private Dictionary<string, ProductResult>? _barcodeMap;
        private Dictionary<string, ProductCatalogResult>? _barcodeCatalogMap;
        private CashSessionResult? _activeCashSession;
        private bool _isPosLoaded;
        private bool _isCashSessionLoaded;
        private DateTime? _lastBootstrapAt;

        // ── Public read-only properties ──────────────────────────────────────────
        public List<ProductResult>? Products => _products;
        public List<ProductCatalogResult>? ProductsCatalog => _productsCatalog;
        public List<ProductCategoryResult>? ProductsCategory => _productsCategory;
        public List<CustomerResult>? Customers => _customers;
        public List<SupplierResult>? Suppliers => _suppliers;
        public Dictionary<Guid, StockResult>? StockMap => _stockMap;
        public Dictionary<Guid, CustomerResult>? CustomerMap => _customerMap;
        public Dictionary<Guid, SupplierResult>? SupplierMap => _supplierMap;
        public Dictionary<Guid, ProductResult>? ProductMap => _productMap;
        public Dictionary<Guid, ProductCatalogResult>? ProductCatalogMap => _productCatalogMap;
        public Dictionary<Guid, ProductCategoryResult>? ProductCategoryMap => _productCategoryMap;
        public Dictionary<string, ProductResult>? BarcodeMap => _barcodeMap;
        public Dictionary<string, ProductCatalogResult>? BarcodeCatalogMap => _barcodeCatalogMap;
        public CashSessionResult? ActiveCashSession => _activeCashSession;
        public bool IsPosLoaded => _isPosLoaded;
        public bool IsCashSessionLoaded => _isCashSessionLoaded;
        public DateTime? LastBootstrapAt => _lastBootstrapAt;

        // ── Computed helpers ─────────────────────────────────────────────────────
        public bool IsFullyLoaded =>
            _isPosLoaded &&
            _products != null &&
            _customers != null &&
            _customerMap != null &&
            _suppliers != null &&
            _supplierMap != null &&
            _stockMap != null &&
            _productMap != null &&
            _productsCatalog != null &&
            _productCatalogMap != null &&
            _productsCategory != null &&
            _productCategoryMap != null &&
            _barcodeMap != null;

        public bool HasInvalidProductCache =>
            _products != null &&
            _products.Any(p =>
                p.CatalogProductId != Guid.Empty &&
                string.IsNullOrWhiteSpace(p.CatalogName));

        public bool IsCacheStale =>
            !IsFullyLoaded ||
            HasInvalidProductCache ||
            _lastBootstrapAt == null ||
            DateTime.UtcNow - _lastBootstrapAt > CacheDuration;

        public bool IsSuppliersLoaded => _suppliers != null && _supplierMap != null;
        public bool IsCustomersLoaded => _customers != null && _customerMap != null;

        // ── Typed lookup helpers (safe, no null-ref) ─────────────────────────────
        public ProductResult? GetProduct(Guid id) => _productMap?.GetValueOrDefault(id);
        public ProductCatalogResult? GetCatalog(Guid id) => _productCatalogMap?.GetValueOrDefault(id);
        public ProductCatalogResult? GetCatalogByBarcode(string barcode) => _barcodeCatalogMap?.GetValueOrDefault(barcode);
        public ProductResult? GetProductByBarcode(string barcode) => _barcodeMap?.GetValueOrDefault(barcode);
        public StockResult? GetStock(Guid productId) => _stockMap?.GetValueOrDefault(productId);
        public CustomerResult? GetCustomer(Guid id) => _customerMap?.GetValueOrDefault(id);
        public SupplierResult? GetSupplier(Guid id) => _supplierMap?.GetValueOrDefault(id);

        // ── Bulk setters (called from your bootstrap / load service) ─────────────

        /// <summary>
        /// Call this once your bootstrap service has fetched everything.
        /// Pass all data at once so the state is always consistent.
        /// </summary>
        public async Task SetPosDataAsync(
            List<ProductResult> products,
            List<ProductCatalogResult> catalogs,
            List<ProductCategoryResult> categories,
            List<CustomerResult> customers,
            List<SupplierResult> suppliers,
            Dictionary<Guid, StockResult> stockMap)
        {
            await _lock.WaitAsync();
            try
            {
                _products = products;
                _productsCatalog = catalogs;
                _productsCategory = categories;
                _customers = customers;
                _suppliers = suppliers;
                _stockMap = stockMap;

                // Build all lookup maps in one pass
                _productMap = products.ToDictionary(p => p.Id);
                _productCatalogMap = catalogs.ToDictionary(c => c.Id);
                _productCategoryMap = categories.ToDictionary(c => c.Id);
                _customerMap = customers.ToDictionary(c => c.Id);
                _supplierMap = suppliers.ToDictionary(s => s.Id);

                _barcodeMap = products
                    .Where(p => !string.IsNullOrWhiteSpace(p.CatalogBarcode))
                    .GroupBy(p => p.CatalogBarcode!)
                    .ToDictionary(g => g.Key, g => g.First());

                _barcodeCatalogMap = catalogs
                    .Where(c => !string.IsNullOrWhiteSpace(c.Barcode))
                    .GroupBy(c => c.Barcode!)
                    .ToDictionary(g => g.Key, g => g.First());

                _isPosLoaded = true;
                _lastBootstrapAt = DateTime.UtcNow;
            }
            finally
            {
                _lock.Release();
            }

            NotifyChange();
        }

        public async Task SetCashSessionAsync(CashSessionResult? session)
        {
            await _lock.WaitAsync();
            try
            {
                _activeCashSession = session;
                _isCashSessionLoaded = true;
            }
            finally { _lock.Release(); }

            NotifyChange();
        }

        // ── Optimistic stock update ──────────────────────────────────────────────
        public void DeductStock(Guid productId, decimal quantity)
        {
            // No await needed — simple field mutation on a reference type is atomic enough
            // for an optimistic UI update; the next bootstrap will correct it if wrong.
            if (_stockMap == null || !_stockMap.TryGetValue(productId, out var stock))
                return;

            stock.Quantity = Math.Max(0, stock.Quantity - quantity);
            NotifyChange();
        }

        // ── Partial invalidation helpers ─────────────────────────────────────────

        /// <summary>Invalidate only the POS product/stock data (e.g. after editing a catalog).</summary>
        public void InvalidatePos()
        {
            _isPosLoaded = false;
            _lastBootstrapAt = null;
            _products = null;
            _productsCatalog = null;
            _productsCategory = null;
            _stockMap = null;
            _productMap = null;
            _productCatalogMap = null;
            _productCategoryMap = null;
            _barcodeMap = null;
            _barcodeCatalogMap = null;
            NotifyChange();
        }

        /// <summary>Invalidate only the customer cache (e.g. after creating a customer).</summary>
        public void InvalidateCustomers()
        {
            _customers = null;
            _customerMap = null;
            NotifyChange();
        }

        /// <summary>Invalidate only the supplier cache.</summary>
        public void InvalidateSuppliers()
        {
            _suppliers = null;
            _supplierMap = null;
            NotifyChange();
        }

        /// <summary>Full reset — use on logout / session end.</summary>
        public void InvalidateAll()
        {
            _isPosLoaded = false;
            _isCashSessionLoaded = false;
            _lastBootstrapAt = null;
            _products = null;
            _productsCatalog = null;
            _productsCategory = null;
            _customers = null;
            _suppliers = null;
            _stockMap = null;
            _customerMap = null;
            _supplierMap = null;
            _productMap = null;
            _productCatalogMap = null;
            _productCategoryMap = null;
            _barcodeMap = null;
            _barcodeCatalogMap = null;
            _activeCashSession = null;
            NotifyChange();
        }

        // ── Private ──────────────────────────────────────────────────────────────
        private void NotifyChange() => OnChange?.Invoke();
    }
}
