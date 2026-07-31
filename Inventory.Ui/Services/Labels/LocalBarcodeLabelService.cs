
using Inventory.LocalDB.Context;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Ui.Services.Labels
{
    public sealed class LocalBarcodeLabelService
    : ILocalBarcodeLabelService
    {
        private readonly PosLocalDbContext _dbContext;

        public LocalBarcodeLabelService(
            PosLocalDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<byte[]> GenerateAsync(
            Guid localProductId,
            CancellationToken cancellationToken = default)
        {
            var product =
                await _dbContext.Products
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        product =>
                            product.Id == localProductId &&
                            !product.IsDeletedLocally,
                        cancellationToken);

            if (product is null)
            {
                throw new KeyNotFoundException(
                    $"Local product '{localProductId}' was not found in SQLite.");
            }

            if (string.IsNullOrWhiteSpace(product.Barcode))
            {
                throw new InvalidOperationException(
                    $"Product '{product.Name}' has no barcode.");
            }

            var label =
                new ProductLabelData(
                    product.Name,
                    product.Brand ?? string.Empty,
                    product.Barcode,
                    product.SalePrice);

            return ProductLabelPdfRenderer.Generate(
                label);
        }
    }
}
