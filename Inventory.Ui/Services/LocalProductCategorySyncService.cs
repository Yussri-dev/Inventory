using Inventory.Dto.ProductCategory.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Inventory.Ui.Services
{
    public sealed class LocalProductCategorySyncService
    : ILocalProductCategorySyncService
    {
        private readonly PosLocalDbContext _dbContext;
        private readonly IProductCategoryApi _categoryApi;

        public LocalProductCategorySyncService(
            PosLocalDbContext dbContext,
            IProductCategoryApi categoryApi)
        {
            _dbContext = dbContext;
            _categoryApi = categoryApi;
        }

        public async Task FullSyncAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var syncStartedAtUtc = DateTime.UtcNow;

            try
            {
                var response = await _categoryApi.GetAll();

                var serverCategories = response?
                    .ToList()
                    ?? new List<ProductCategoryResult>();

                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync(
                        cancellationToken);

                try
                {
                    var serverIds = serverCategories
                        .Select(x => x.Id)
                        .ToList();

                    var existingCategories =
                        await _dbContext.ProductCategories
                            .Where(x => serverIds.Contains(x.Id))
                            .ToDictionaryAsync(
                                x => x.Id,
                                cancellationToken);

                    foreach (var serverCategory in serverCategories)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!existingCategories.TryGetValue(
                                serverCategory.Id,
                                out var localCategory))
                        {
                            localCategory = new LocalProductCategory
                            {
                                Id = serverCategory.Id
                            };

                            await _dbContext.ProductCategories.AddAsync(
                                localCategory,
                                cancellationToken);

                            existingCategories[serverCategory.Id] =
                                localCategory;
                        }

                        MapToLocal(
                            serverCategory,
                            localCategory,
                            syncStartedAtUtc);
                    }

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);

                    await MarkMissingCategoriesAsDeletedAsync(
                        syncStartedAtUtc,
                        cancellationToken);

                    await transaction.CommitAsync(
                        cancellationToken);

                    _dbContext.ChangeTracker.Clear();

                    Debug.WriteLine(
                        $"Product category sync completed. " +
                        $"Downloaded: {serverCategories.Count}.");
                }
                catch
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                _dbContext.ChangeTracker.Clear();
                throw;
            }
            catch (Exception exception)
            {
                _dbContext.ChangeTracker.Clear();

                Debug.WriteLine(
                    $"Product category synchronization failed: {exception}");

                throw new InvalidOperationException(
                    "Product category synchronization failed.",
                    exception);
            }
        }

        public async Task UpsertAsync(
            ProductCategoryResult category,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (category.Id == Guid.Empty)
            {
                throw new ArgumentException(
                    "The category ID cannot be empty.",
                    nameof(category));
            }

            var localCategory = await _dbContext.ProductCategories
                .FirstOrDefaultAsync(
                    x => x.Id == category.Id,
                    cancellationToken);

            if (localCategory == null)
            {
                localCategory = new LocalProductCategory
                {
                    Id = category.Id
                };

                await _dbContext.ProductCategories.AddAsync(
                    localCategory,
                    cancellationToken);
            }

            MapToLocal(
                category,
                localCategory,
                DateTime.UtcNow);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        public async Task MarkDeletedAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            if (categoryId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The category ID cannot be empty.",
                    nameof(categoryId));
            }

            var localCategory = await _dbContext.ProductCategories
                .FirstOrDefaultAsync(
                    x => x.Id == categoryId,
                    cancellationToken);

            if (localCategory == null)
                return;

            localCategory.IsDeleted = true;
            localCategory.LastSyncedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        private async Task MarkMissingCategoriesAsDeletedAsync(
            DateTime syncStartedAtUtc,
            CancellationToken cancellationToken)
        {
            await _dbContext.ProductCategories
                .Where(x =>
                    !x.IsDeleted &&
                    x.LastSyncedAtUtc < syncStartedAtUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.IsDeleted,
                            true)
                        .SetProperty(
                            x => x.LastSyncedAtUtc,
                            syncStartedAtUtc),
                    cancellationToken);
        }

        private static void MapToLocal(
            ProductCategoryResult source,
            LocalProductCategory destination,
            DateTime syncedAtUtc)
        {
            destination.Name = string.IsNullOrWhiteSpace(source.Name)
                ? "Unnamed category"
                : source.Name.Trim();

            destination.DisplayOrder = source.DisplayOrder;

            destination.Color = string.IsNullOrWhiteSpace(source.Color)
                ? null
                : source.Color.Trim();

            destination.Icon = string.IsNullOrWhiteSpace(source.Icon)
                ? null
                : source.Icon.Trim();

            destination.IsDeleted = false;
            destination.LastSyncedAtUtc = syncedAtUtc;
        }
    }
}
