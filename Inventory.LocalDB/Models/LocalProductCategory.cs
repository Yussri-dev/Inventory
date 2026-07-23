using System.ComponentModel.DataAnnotations;

namespace Inventory.LocalDB.Models
{
    public sealed class LocalProductCategory
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(100)]
        public string? Icon { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime LastSyncedAtUtc { get; set; }
    }
}
