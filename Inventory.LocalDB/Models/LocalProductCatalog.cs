using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;

namespace Inventory.LocalDB.Models
{
    public class LocalProductCatalog
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string? Barcode { get; set; }

        [MaxLength(50)]
        public string InternalCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(100)]
        public string? Manufacturer { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public Guid CategoryId { get; set; }

        public SellingMode SellingMode { get; set; }

        [MaxLength(20)]
        public string UnitOfMeasure { get; set; } = "pcs";

        public bool IsPack { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? ServerCreatedAt { get; set; }
        public DateTime? ServerModifiedAt { get; set; }

        public DateTime LastSyncedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<LocalPackComponent> PackComponents { get; set; } = new List<LocalPackComponent>();
    }
}
