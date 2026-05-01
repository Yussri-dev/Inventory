using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.ProductCategory.Requests
{
    public class UpdateProductCategoryRequest
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public int DisplayOrder { get; set; }

        public string? Color { get; set; }

        public string? Icon { get; set; }

    }
}
