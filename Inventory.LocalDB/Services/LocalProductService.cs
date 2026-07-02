using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Services
{
    public class LocalProductService : ILocalProductService
    {
        private readonly PosLocalDbContext _db;

        public LocalProductService(PosLocalDbContext db)
        {
            _db = db;
        }

        public async Task<LocalProduct?> GetByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return null;

            var normalized = barcode.Trim();

            return await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Barcode == normalized &&
                    x.IsActive &&
                    !x.IsDeletedLocally);
        }

        public async Task<LocalProductScanResult?> ResolveBarcodeAsync(string barcode)
        {
            var product = await GetByBarcodeAsync(barcode);

            if (product == null)
                return null;

            if (!product.IsPack)
            {
                return new LocalProductScanResult
                {
                    ProductLocalId = product.Id,
                    ProductServerId = product.ServerId,
                    ProductName = product.Name,
                    ProductBarcode = product.Barcode,

                    UnitProductLocalId = product.Id,
                    UnitProductServerId = product.ServerId,
                    UnitProductName = product.Name,
                    UnitProductBarcode = product.Barcode,

                    IsPack = false,
                    Quantity = 1,
                    UnitQuantity = 1,

                    UnitPrice = product.SalePrice,
                    PurchasePrice = product.PurchasePrice,
                    VatRate = product.VatRate
                };
            }

            if (product.UnitProductServerId == null)
                throw new InvalidOperationException($"Pack '{product.Name}' is not linked to a unit product.");

            var unitProduct = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ServerId == product.UnitProductServerId &&
                    x.IsActive &&
                    !x.IsDeletedLocally);

            if (unitProduct == null)
                throw new InvalidOperationException($"Unit product for pack '{product.Name}' was not found locally.");

            return new LocalProductScanResult
            {
                ProductLocalId = product.Id,
                ProductServerId = product.ServerId,
                ProductName = product.Name,
                ProductBarcode = product.Barcode,

                UnitProductLocalId = unitProduct.Id,
                UnitProductServerId = unitProduct.ServerId,
                UnitProductName = unitProduct.Name,
                UnitProductBarcode = unitProduct.Barcode,

                IsPack = true,
                Quantity = 1,
                UnitQuantity = product.UnitsPerPack,

                UnitPrice = product.SalePrice,
                PurchasePrice = unitProduct.PurchasePrice,
                VatRate = product.VatRate
            };
        }

        public async Task UpsertAsync(LocalProduct product)
        {
            var existing = await _db.Products
                .FirstOrDefaultAsync(x => x.ServerId == product.ServerId);

            if (existing == null)
            {
                product.Id = product.Id == Guid.Empty ? Guid.NewGuid() : product.Id;
                product.LastSyncedAtUtc = DateTime.UtcNow;

                _db.Products.Add(product);
            }
            else
            {
                existing.CatalogProductId = product.CatalogProductId;
                existing.Name = product.Name;
                existing.Sku = product.Sku;
                existing.Barcode = product.Barcode;
                existing.Category = product.Category;
                existing.Brand = product.Brand;
                existing.SalePrice = product.SalePrice;
                existing.SalePrice2 = product.SalePrice2;
                existing.SalePrice3 = product.SalePrice3;
                existing.PurchasePrice = product.PurchasePrice;
                existing.VatRate = product.VatRate;
                existing.Unit = product.Unit;
                existing.IsActive = product.IsActive;
                existing.IsTracked = product.IsTracked;
                existing.LocalStockQuantity = product.LocalStockQuantity;
                existing.IsPack = product.IsPack;
                existing.UnitProductServerId = product.UnitProductServerId;
                existing.UnitsPerPack = product.UnitsPerPack;
                existing.IsDeletedLocally = product.IsDeletedLocally;
                existing.LastSyncedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        public async Task<List<LocalProduct>> SearchAsync(string search, int take = 50)
        {
            var query = _db.Products
                .AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeletedLocally);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.Name.Contains(term) ||
                    (x.Barcode != null && x.Barcode.Contains(term)) ||
                    (x.Sku != null && x.Sku.Contains(term)));
            }

            return await query
                .OrderBy(x => x.Name)
                .Take(take)
                .ToListAsync();
        }
    }
}
