using Inventory.LocalDB.Models;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalProductService
    {
        Task<LocalProduct?> GetByBarcodeAsync(string barcode);
        Task<LocalProductScanResult?> ResolveBarcodeAsync(string barcode);
        Task UpsertAsync(LocalProduct product);
        Task<List<LocalProduct>> SearchAsync(string search, int take = 50);
    }
}
