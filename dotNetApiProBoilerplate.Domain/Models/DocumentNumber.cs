
using Inventory.Domain.Abstraction;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Models
{
    public class DocumentNumber : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string DocumentType { get; set; } = null!; // Sale, Purchase, Return, etc.

        [Required, MaxLength(20)]
        public string Prefix { get; set; } = null!;

        public int LastNumber { get; set; }

        public int PaddingLength { get; set; } = 6;

        [MaxLength(20)]
        public string? Suffix { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        public bool ResetYearly { get; set; }
        public bool ResetMonthly { get; set; }
    }

}
