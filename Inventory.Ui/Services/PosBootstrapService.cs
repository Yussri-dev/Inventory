using Inventory.Ui.Interfaces;
using Inventory.Ui.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.Services
{
    public class PosBootstrapService
    {
        private readonly AppState _state;
        private readonly IProductApi _productApi;
        private readonly IProductCatalogApi _catalogApi;
        private readonly IProductCategoryApi _categoryApi;
        private readonly ICustomerApi _customerApi;
        private readonly ISupplierApi _supplierApi;
        private readonly IStockApi _stockApi;

        // Prevent two pages from bootstrapping simultaneously
        private readonly SemaphoreSlim _bootstrapLock = new(1, 1);

        public PosBootstrapService(
            AppState state,
            IProductApi productApi,
            IProductCatalogApi catalogApi,
            IProductCategoryApi categoryApi,
            ICustomerApi customerApi,
            ISupplierApi supplierApi,
            IStockApi stockApi)
        {
            _state = state;
            _productApi = productApi;
            _catalogApi = catalogApi;
            _categoryApi = categoryApi;
            _customerApi = customerApi;
            _supplierApi = supplierApi;
            _stockApi = stockApi;
        }

        /// <summary>
        /// Loads data only when the cache is stale or empty.
        /// Safe to call from multiple components at once.
        /// </summary>
        public async Task EnsureLoadedAsync(CancellationToken ct = default)
        {
            if (!_state.IsCacheStale) return;

            await _bootstrapLock.WaitAsync(ct);
            try
            {
                // Double-check after acquiring the lock
                if (!_state.IsCacheStale) return;

                await LoadAllAsync(ct);
            }
            finally
            {
                _bootstrapLock.Release();
            }
        }

        /// <summary>Force a full reload regardless of staleness.</summary>
        public async Task ForceReloadAsync(CancellationToken ct = default)
        {
            _state.InvalidateAll();
            await EnsureLoadedAsync(ct);
        }

        private async Task LoadAllAsync(CancellationToken ct)
        {
            // Fetch everything in parallel
            var productsTask = _productApi.GetAll();
            var catalogsTask = _catalogApi.Search(new() { Page = 1, PageSize = 1000 });
            var categoriesTask = _categoryApi.GetAll();
            var customersTask = _customerApi.GetAll();
            var suppliersTask = _supplierApi.GetAll();
            var stockTask = _stockApi.GetAll();

            await Task.WhenAll(productsTask, catalogsTask, categoriesTask,
                               customersTask, suppliersTask, stockTask);

            var products = (await productsTask)?.ToList() ?? new();
            var catalogs = (await catalogsTask).Items?.ToList() ?? new();
            var categories = (await categoriesTask)?.ToList() ?? new();
            var customers = (await customersTask)?.ToList() ?? new();
            var suppliers = (await suppliersTask)?.ToList() ?? new();
            var stockList = (await stockTask)?.ToList() ?? new();

            var stockMap = stockList.ToDictionary(s => s.ProductId);

            await _state.SetPosDataAsync(
                products, catalogs, categories,
                customers, suppliers, stockMap);
        }
    }
}
