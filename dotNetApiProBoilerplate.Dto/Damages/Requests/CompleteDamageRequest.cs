using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Damages.Requests
{
    public sealed class CompleteDamageRequest
    {
        [Required]
        public Guid ClientOperationId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Range(
            typeof(decimal),
            "0.001",
            "999999999")]
        public decimal Quantity { get; set; }

        [MaxLength(1000)]
        public string? Reason { get; set; }

        public DateTime DamageDate { get; set; }
    }
}
