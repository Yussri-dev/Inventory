using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class PackService : IPackService
    {
        private readonly IRepository<ProductCatalog> _catalogRepository;
        private readonly IRepository<PackComponent> _packComponentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenantContext;
        private readonly Dictionary<Guid, PackCacheEntry> _cache = new();

        public PackService(
            IRepository<ProductCatalog> catalogRepository,
            IRepository<PackComponent> packComponentRepository,
            IUnitOfWork unitOfWork,
            ITenantContext tenantContext)
        {
            _catalogRepository = catalogRepository;
            _packComponentRepository = packComponentRepository;
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
        }

        public bool IsPack(Guid catalogProductId)
            => GetCacheEntry(catalogProductId).IsPack;

        public Guid? GetComponentCatalogId(Guid catalogProductId)
        {
            var entry = GetCacheEntry(catalogProductId);
            return entry.IsPack ? entry.ComponentCatalogId : null;
        }

        public decimal GetPackSize(Guid catalogProductId)
        {
            var entry = GetCacheEntry(catalogProductId);
            return entry.IsPack ? entry.PackSize : 1m;
        }

        public decimal GetUnitQuantity(Guid catalogProductId, decimal quantity)
            => quantity * GetPackSize(catalogProductId);

        public void InvalidateCache(Guid catalogProductId)
            => _cache.Remove(catalogProductId);

        // ---

        private PackCacheEntry GetCacheEntry(Guid catalogProductId)
        {
            if (_cache.TryGetValue(catalogProductId, out var cached))
                return cached;

            var catalog = _catalogRepository
                .Query()
                .Include(c => c.PackComponents)
                .FirstOrDefault(c => c.Id == catalogProductId);

            var entry = BuildCacheEntry(catalog);
            _cache[catalogProductId] = entry;
            return entry;
        }

        private static PackCacheEntry BuildCacheEntry(ProductCatalog? catalog)
        {
            if (catalog == null || !catalog.IsPack || !catalog.PackComponents.Any())
                return new PackCacheEntry { IsPack = false, PackSize = 1m };

            var component = catalog.PackComponents.First();

            return new PackCacheEntry
            {
                IsPack = true,
                PackSize = component.Quantity,
                ComponentCatalogId = component.ComponentCatalogId
            };
        }

        private class PackCacheEntry
        {
            public bool IsPack { get; set; }
            public decimal PackSize { get; set; } = 1m;
            public Guid ComponentCatalogId { get; set; }
        }
    }
}