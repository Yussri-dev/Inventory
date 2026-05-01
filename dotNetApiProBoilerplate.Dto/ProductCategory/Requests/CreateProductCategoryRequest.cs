

namespace Inventory.Dto.ProductCategory.Requests
{
    public class CreateProductCategoryRequest
    {

        public string Name { get; set; }

        public int DisplayOrder { get; set; }

        public string? Color { get; set; }

        public string? Icon { get; set; }

    }
}
