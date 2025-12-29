using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Suppliers.Requests
{
    public class UpdateSupplierRequest
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

        public int PaymentTermsDays { get; set; }

        [MaxLength(50)]
        public string? BankAccount { get; set; }

        public bool IsActive { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
