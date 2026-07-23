using Inventory.Dto.ProductCategory.Requests;
using Inventory.Dto.ProductCategory.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services
{
    public class LocalProductCategoryService : ILocalProductCategoryService
    {
        private readonly PosLocalDbContext _dbcontext;
        public LocalProductCategoryService(PosLocalDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task<ProductCategoryResult> CreateAsync(
            CreateProductCategoryRequest request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("product category name is required.");
            }

            var normalizedName = request.Name.Trim().ToLower();

            var exists = await _dbcontext.ProductCategories
                                .AnyAsync(x => x.Name.ToLower() == normalizedName, ct);

            if (exists)
            {
                throw new InvalidOperationException($"Product category '{request.Name}' ");
            }

            var productCategory = new LocalProductCategory
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DisplayOrder = request.DisplayOrder,
                Color = request.Color,
                Icon = request.Icon
            };

            _dbcontext.ProductCategories.Add(productCategory);

            await _dbcontext.SaveChangesAsync(ct);

            return ToResult(productCategory);
        }

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<ProductCategoryResult>> GetAllAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<ProductCategoryResult> UpdateAsync(Guid id, UpdateProductCategoryRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        private static ProductCategoryResult ToResult(LocalProductCategory productCategory)
        {
            return new ProductCategoryResult
            {
                Id = productCategory.Id,
                Name = productCategory.Name,
                DisplayOrder = productCategory.DisplayOrder,
                Color = productCategory.Color,
                Icon = productCategory.Icon,
            };
        }
    }
}