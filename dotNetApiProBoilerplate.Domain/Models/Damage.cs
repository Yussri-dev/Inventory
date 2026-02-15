
using Inventory.Domain.Abstraction;
using Inventory.Domain.Entities;
using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Models
{
    public class Damage : TenantEntity
    {
        public Guid Id { get; set; }

        public string DamageNumber { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public decimal Quantity { get; set; }
        public decimal EstimatedValue { get; set; }

        public string Reason { get; set; } = null!;
        public string? Category { get; set; }

        public DateTime DamageDate { get; set; }

        public DamageStatus Status { get; set; }

        public DateTime? ValidatedAt { get; set; }
        public Guid? ValidatedByUserId { get; set; }
    }


}
