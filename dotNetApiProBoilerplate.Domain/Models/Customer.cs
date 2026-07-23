
using Inventory.Domain.Abstraction;
using Inventory.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class Customer : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; }

        /*
         * Authoritative customer account balance:
         *   positive => customer owes the store
         *   negative => store owes the customer
         *   zero     => settled
         */
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public ICollection<Sale> Sales { get; set; } =
            new List<Sale>();

        public ICollection<LoyaltyCard> LoyaltyCards { get; set; } =
            new List<LoyaltyCard>();

        public ICollection<CustomerTransaction> Transactions { get; set; } =
            new List<CustomerTransaction>();
    }
}
