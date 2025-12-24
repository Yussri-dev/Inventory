
using Inventory.Domain.Abstraction;
using Inventory.Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Entities
{
    public class Supplier : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        public int PaymentTermsDays { get; set; } // Délai de paiement en jours

        [MaxLength(50)]
        public string? BankAccount { get; set; }

        public bool IsActive { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public ICollection<SupplierReturn> SupplierReturns { get; set; } = new List<SupplierReturn>();
    }
}
