namespace Inventory.Dto.ProductCatalogs.Results
{
    public class ProductCatalogResult
    {
        public Guid Id { get; set; }

        public string Barcode { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Brand { get; set; }

        public string? Manufacturer { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
