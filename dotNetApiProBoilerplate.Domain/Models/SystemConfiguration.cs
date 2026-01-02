
using Inventory.Domain.Abstraction;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Models
{
    // ===============================
    // 15. CONFIGURATIONS
    // ===============================

    public class SystemConfiguration : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string Key { get; set; } = null!;

        [Required]
        public string Value { get; set; } = null!;

        [MaxLength(50)]
        public string DataType { get; set; } = "String"; // String, Int, Decimal, Boolean, JSON

        [MaxLength(200)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsEditable { get; set; } = true;

        public DateTime LastModified { get; set; }
    }

}
