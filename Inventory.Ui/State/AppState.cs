using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.ProductCategory.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Stock.Results;
using Inventory.Dto.Suppliers.Results;

namespace Inventory.Ui.State
{
    public class AppState
    {
        // ============================================================
        // CHANGE NOTIFICATION
        // ============================================================

        public event Action? OnChange;

        // ============================================================
        // INTERNAL LOCK
        // ============================================================

        private readonly SemaphoreSlim _lock =
            new(1, 1);

        // ============================================================
        // CACHE DURATION
        // ============================================================

        private static readonly TimeSpan CacheDuration =
            TimeSpan.FromMinutes(10);

        // ============================================================
        // BACKING FIELDS
        // ============================================================

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

        /*
         * Barcode must identify exactly one tenant Product.
         *
         * IMPORTANT:
         * We intentionally DO NOT use GroupBy(...).First().
         *
         * Duplicate barcodes are considered invalid data and must
         * be corrected rather than silently selecting a random product.
         */
        private Dictionary<string, ProductResult>? _barcodeMap;

        private Dictionary<string, ProductCatalogResult>? _barcodeCatalogMap;

        private CashSessionResult? _activeCashSession;

        private bool _isPosLoaded;

        private bool _isCashSessionLoaded;

        private DateTime? _lastBootstrapAt;

        // ============================================================
        // PUBLIC READ-ONLY PROPERTIES
        // ============================================================

        public List<ProductResult>? Products =>
            _products;

        public List<ProductCatalogResult>? ProductsCatalog =>
            _productsCatalog;

        public List<ProductCategoryResult>? ProductsCategory =>
            _productsCategory;

        public List<CustomerResult>? Customers =>
            _customers;

        public List<SupplierResult>? Suppliers =>
            _suppliers;

        public Dictionary<Guid, StockResult>? StockMap =>
            _stockMap;

        public Dictionary<Guid, CustomerResult>? CustomerMap =>
            _customerMap;

        public Dictionary<Guid, SupplierResult>? SupplierMap =>
            _supplierMap;

        public Dictionary<Guid, ProductResult>? ProductMap =>
            _productMap;

        public Dictionary<Guid, ProductCatalogResult>? ProductCatalogMap =>
            _productCatalogMap;

        public Dictionary<Guid, ProductCategoryResult>? ProductCategoryMap =>
            _productCategoryMap;

        public Dictionary<string, ProductResult>? BarcodeMap =>
            _barcodeMap;

        public Dictionary<string, ProductCatalogResult>? BarcodeCatalogMap =>
            _barcodeCatalogMap;

        public CashSessionResult? ActiveCashSession =>
            _activeCashSession;

        public bool IsPosLoaded =>
            _isPosLoaded;

        public bool IsCashSessionLoaded =>
            _isCashSessionLoaded;

        public DateTime? LastBootstrapAt =>
            _lastBootstrapAt;

        // ============================================================
        // COMPUTED HELPERS
        // ============================================================

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
     _barcodeMap != null &&
     _barcodeCatalogMap != null;


        private static void ValidateUniqueCatalogBarcodes(
    IEnumerable<ProductCatalogResult> catalogs)
        {
            var duplicates =
                catalogs
                    .Where(catalog =>
                        !string.IsNullOrWhiteSpace(
                            catalog.Barcode))
                    .GroupBy(
                        catalog =>
                            NormalizeBarcode(
                                catalog.Barcode!),
                        StringComparer.OrdinalIgnoreCase)
                    .Where(group =>
                        group.Count() > 1)
                    .Select(group =>
                        new
                        {
                            Barcode =
                                group.Key,

                            Catalogs =
                                group
                                    .Select(catalog =>
                                        $"{catalog.Name} ({catalog.Id})")
                                    .ToList()
                        })
                    .ToList();

            if (duplicates.Count == 0)
            {
                return;
            }

            var details =
                string.Join(
                    "; ",
                    duplicates.Select(
                        duplicate =>
                            $"{duplicate.Barcode}: " +
                            string.Join(
                                ", ",
                                duplicate.Catalogs)));

            throw new InvalidOperationException(
                "Duplicate catalog barcodes were detected. " +
                "A barcode must identify exactly one catalog product. " +
                $"Duplicates: {details}");
        }


        /// <summary>
        /// Vérifie si le cache contient un Product invalide.
        ///
        /// Un custom Product avec CatalogProductId = null est valide.
        ///
        /// Invalid:
        /// - Product Id == Guid.Empty
        /// - Name vide
        /// - CatalogProductId == Guid.Empty lorsqu'il est présent
        /// </summary>
        public bool HasInvalidProductCache =>
            _products != null &&
            _products.Any(product =>
                product.Id == Guid.Empty ||
                string.IsNullOrWhiteSpace(product.Name) ||
                (
                    product.CatalogProductId.HasValue &&
                    product.CatalogProductId.Value == Guid.Empty
                ));

        public bool IsCacheStale =>
            !IsFullyLoaded ||
            HasInvalidProductCache ||
            _lastBootstrapAt == null ||
            DateTime.UtcNow -
                _lastBootstrapAt.Value >
                CacheDuration;

        public bool IsSuppliersLoaded =>
            _suppliers != null &&
            _supplierMap != null;

        public bool IsCustomersLoaded =>
            _customers != null &&
            _customerMap != null;

        // ============================================================
        // PRODUCT LOOKUP
        // ============================================================

        public ProductResult? GetProduct(
            Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            return _productMap?
                .GetValueOrDefault(id);
        }

        // ============================================================
        // PRODUCT CATALOG LOOKUP
        // ============================================================

        public ProductCatalogResult? GetCatalog(
            Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            return _productCatalogMap?
                .GetValueOrDefault(id);
        }

        public ProductCatalogResult? GetCatalog(
            Guid? id)
        {
            if (!id.HasValue ||
                id.Value == Guid.Empty)
            {
                return null;
            }

            return _productCatalogMap?
                .GetValueOrDefault(
                    id.Value);
        }

        // ============================================================
        // BARCODE LOOKUPS
        // ============================================================

        public ProductResult? GetProductByBarcode(
            string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }

            var normalizedBarcode =
                NormalizeBarcode(barcode);

            return _barcodeMap?
                .GetValueOrDefault(
                    normalizedBarcode);
        }

        public ProductCatalogResult? GetCatalogByBarcode(
            string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }

            var normalizedBarcode =
                NormalizeBarcode(barcode);

            return _barcodeCatalogMap?
                .GetValueOrDefault(
                    normalizedBarcode);
        }

        // ============================================================
        // STOCK LOOKUP
        // ============================================================

        public StockResult? GetStock(
            Guid productId)
        {
            if (productId == Guid.Empty)
            {
                return null;
            }

            return _stockMap?
                .GetValueOrDefault(
                    productId);
        }

        // ============================================================
        // CUSTOMER LOOKUP
        // ============================================================

        public CustomerResult? GetCustomer(
            Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            return _customerMap?
                .GetValueOrDefault(id);
        }

        // ============================================================
        // SUPPLIER LOOKUP
        // ============================================================

        public SupplierResult? GetSupplier(
            Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            return _supplierMap?
                .GetValueOrDefault(id);
        }

        // ============================================================
        // SET POS DATA
        // ============================================================

        public async Task SetPosDataAsync(
     List<ProductResult> products,
     List<ProductCatalogResult> catalogs,
     List<ProductCategoryResult> categories,
     List<CustomerResult> customers,
     List<SupplierResult> suppliers,
     Dictionary<Guid, StockResult> stockMap)
        {
            ArgumentNullException.ThrowIfNull(products);
            ArgumentNullException.ThrowIfNull(catalogs);
            ArgumentNullException.ThrowIfNull(categories);
            ArgumentNullException.ThrowIfNull(customers);
            ArgumentNullException.ThrowIfNull(suppliers);
            ArgumentNullException.ThrowIfNull(stockMap);

            await _lock.WaitAsync();

            try
            {
                // ============================================================
                // VALIDATION
                // ============================================================

                ValidateUniqueProductBarcodes(
                    products);

                ValidateUniqueCatalogBarcodes(
                    catalogs);

                // ============================================================
                // MAIN DATA
                // ============================================================

                _products =
                    products;

                _productsCatalog =
                    catalogs;

                _productsCategory =
                    categories;

                _customers =
                    customers;

                _suppliers =
                    suppliers;

                _stockMap =
                    stockMap;

                // ============================================================
                // ID MAPS
                // ============================================================

                _productMap =
                    products
                        .Where(product =>
                            product.Id != Guid.Empty)
                        .ToDictionary(
                            product =>
                                product.Id);

                _productCatalogMap =
                    catalogs
                        .Where(catalog =>
                            catalog.Id != Guid.Empty)
                        .ToDictionary(
                            catalog =>
                                catalog.Id);

                _productCategoryMap =
                    categories
                        .Where(category =>
                            category.Id != Guid.Empty)
                        .ToDictionary(
                            category =>
                                category.Id);

                _customerMap =
                    customers
                        .Where(customer =>
                            customer.Id != Guid.Empty)
                        .ToDictionary(
                            customer =>
                                customer.Id);

                _supplierMap =
                    suppliers
                        .Where(supplier =>
                            supplier.Id != Guid.Empty)
                        .ToDictionary(
                            supplier =>
                                supplier.Id);

                // ============================================================
                // PRODUCT BARCODE MAP
                // ============================================================

                /*
                 * One barcode = one tenant Product.
                 *
                 * No GroupBy().First().
                 *
                 * Any duplicate is considered invalid data and is detected
                 * by ValidateUniqueProductBarcodes().
                 */
                _barcodeMap =
                    products
                        .Where(product =>
                            !string.IsNullOrWhiteSpace(
                                product.Barcode))
                        .ToDictionary(
                            product =>
                                NormalizeBarcode(
                                    product.Barcode!),

                            product =>
                                product,

                            StringComparer.OrdinalIgnoreCase);

                // ============================================================
                // PRODUCT CATALOG BARCODE MAP
                // ============================================================

                /*
                 * ProductCatalog barcodes have now been cleaned.
                 *
                 * One barcode = one ProductCatalog.
                 *
                 * No GroupBy().First().
                 */
                _barcodeCatalogMap =
                    catalogs
                        .Where(catalog =>
                            !string.IsNullOrWhiteSpace(
                                catalog.Barcode))
                        .ToDictionary(
                            catalog =>
                                NormalizeBarcode(
                                    catalog.Barcode!),

                            catalog =>
                                catalog,

                            StringComparer.OrdinalIgnoreCase);

                // ============================================================
                // CACHE STATUS
                // ============================================================

                _isPosLoaded =
                    true;

                _lastBootstrapAt =
                    DateTime.UtcNow;
            }
            finally
            {
                _lock.Release();
            }

            NotifyChange();
        }

        // ============================================================
        // CASH SESSION
        // ============================================================

        public async Task SetCashSessionAsync(
            CashSessionResult? session)
        {
            await _lock.WaitAsync();

            try
            {
                _activeCashSession =
                    session;

                _isCashSessionLoaded =
                    true;
            }
            finally
            {
                _lock.Release();
            }

            NotifyChange();
        }

        // ============================================================
        // OPTIMISTIC STOCK UPDATE
        // ============================================================

        public void DeductStock(
            Guid productId,
            decimal quantity)
        {
            if (productId == Guid.Empty ||
                quantity <= 0m)
            {
                return;
            }

            if (_stockMap == null ||
                !_stockMap.TryGetValue(
                    productId,
                    out var stock))
            {
                return;
            }

            stock.Quantity =
                Math.Max(
                    0m,
                    stock.Quantity -
                    quantity);

            NotifyChange();
        }

        // ============================================================
        // INVALIDATE POS
        // ============================================================

        public void InvalidatePos()
        {
            _isPosLoaded =
                false;

            _lastBootstrapAt =
                null;

            _products =
                null;

            _productsCatalog =
                null;

            _productsCategory =
                null;

            _stockMap =
                null;

            _productMap =
                null;

            _productCatalogMap =
                null;

            _productCategoryMap =
                null;

            _barcodeMap =
                null;

            _barcodeCatalogMap =
                null;

            NotifyChange();
        }

        // ============================================================
        // INVALIDATE CUSTOMERS
        // ============================================================

        public void InvalidateCustomers()
        {
            _customers =
                null;

            _customerMap =
                null;

            NotifyChange();
        }

        // ============================================================
        // INVALIDATE SUPPLIERS
        // ============================================================

        public void InvalidateSuppliers()
        {
            _suppliers =
                null;

            _supplierMap =
                null;

            NotifyChange();
        }

        // ============================================================
        // FULL RESET
        // ============================================================

        public void InvalidateAll()
        {
            _isPosLoaded =
                false;

            _isCashSessionLoaded =
                false;

            _lastBootstrapAt =
                null;

            _products =
                null;

            _productsCatalog =
                null;

            _productsCategory =
                null;

            _customers =
                null;

            _suppliers =
                null;

            _stockMap =
                null;

            _customerMap =
                null;

            _supplierMap =
                null;

            _productMap =
                null;

            _productCatalogMap =
                null;

            _productCategoryMap =
                null;

            _barcodeMap =
                null;

            _barcodeCatalogMap =
                null;

            _activeCashSession =
                null;

            NotifyChange();
        }

        // ============================================================
        // BARCODE VALIDATION
        // ============================================================

        private static void ValidateUniqueProductBarcodes(
            IEnumerable<ProductResult> products)
        {
            var duplicates =
                products
                    .Where(product =>
                        !string.IsNullOrWhiteSpace(
                            product.Barcode))
                    .GroupBy(
                        product =>
                            NormalizeBarcode(
                                product.Barcode!),

                        StringComparer.OrdinalIgnoreCase)
                    .Where(group =>
                        group.Count() > 1)
                    .Select(group =>
                        new
                        {
                            Barcode =
                                group.Key,

                            Products =
                                group
                                    .Select(product =>
                                        $"{product.Name} ({product.Id})")
                                    .ToList()
                        })
                    .ToList();

            if (duplicates.Count == 0)
            {
                return;
            }

            var details =
                string.Join(
                    "; ",
                    duplicates.Select(
                        duplicate =>
                            $"{duplicate.Barcode}: " +
                            string.Join(
                                ", ",
                                duplicate.Products)));

            throw new InvalidOperationException(
                "Duplicate product barcodes were detected. " +
                "A barcode must identify exactly one tenant product. " +
                $"Duplicates: {details}");
        }

        // ============================================================
        // BARCODE NORMALIZATION
        // ============================================================

        private static string NormalizeBarcode(
            string barcode)
        {
            return barcode.Trim();
        }

        // ============================================================
        // NOTIFICATION
        // ============================================================

        private void NotifyChange()
        {
            OnChange?.Invoke();
        }
    }
}