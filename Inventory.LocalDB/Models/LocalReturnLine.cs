using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models;

public class LocalReturnLine : ILocalTenantEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    [Required]
    public Guid LocalReturnId { get; set; }

    public LocalReturn LocalReturn { get; set; } = null!;

    [Required]
    public Guid LocalSaleLineId { get; set; }

    public Guid? ServerSaleLineId { get; set; }

    [Required]
    public Guid ProductLocalId { get; set; }

    public Guid? ProductServerId { get; set; }

    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ProductBarcode { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal Quantity { get; set; }

    /*
     * Product that actually affects stock.
     * For a pack return this is the linked unit product.
     */
    [Required]
    public Guid UnitProductLocalId { get; set; }

    public Guid? UnitProductServerId { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal UnitQuantity { get; set; }

    public bool IsPack { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal UnitsPerPack { get; set; } = 1m;

    /*
     * Effective VAT-included unit price after original sale discounts.
     */
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal VatRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCostPrice { get; set; }

    [NotMapped]
    public decimal LineAmount =>
        Math.Round(
            Quantity * UnitPrice,
            2,
            MidpointRounding.AwayFromZero);

    [NotMapped]
    public decimal TaxAmount
    {
        get
        {
            if (VatRate <= 0m)
            {
                return 0m;
            }

            var lineExclVat =
                LineAmount /
                (1m + VatRate / 100m);

            return Math.Round(
                LineAmount - lineExclVat,
                2,
                MidpointRounding.AwayFromZero);
        }
    }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public bool RestockItem { get; set; }
}
