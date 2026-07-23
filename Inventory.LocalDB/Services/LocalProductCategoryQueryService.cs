using Inventory.Dto.ProductCategory.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Services
{
    public sealed class LocalProductCategoryQueryService
    : ILocalProductCategoryQueryService
    {
        private readonly PosLocalDbContext _dbContext;

        public LocalProductCategoryQueryService(
            PosLocalDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ProductCategoryResult>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var databasePath = _dbContext.Database
                .GetDbConnection()
                .DataSource;

            Debug.WriteLine(
                $"Local ProductCategory database: {databasePath}");

            var categories = await _dbContext.ProductCategories
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return categories
                .Select(ToResult)
                .ToList();
        }

        public async Task<ProductCategoryResult?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
                return null;

            var category = await _dbContext.ProductCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == id &&
                        !x.IsDeleted,
                    cancellationToken);

            return category == null
                ? null
                : ToResult(category);
        }

        public Task<bool> HasLocalDataAsync(
            CancellationToken cancellationToken = default)
        {
            return _dbContext.ProductCategories
                .AsNoTracking()
                .AnyAsync(
                    x => !x.IsDeleted,
                    cancellationToken);
        }

        private static ProductCategoryResult ToResult(
            LocalProductCategory category)
        {
            return new ProductCategoryResult
            {
                Id = category.Id,
                Name = category.Name,
                DisplayOrder = category.DisplayOrder,
                Color = category.Color,
                Icon = category.Icon
            };
        }
    }
}
