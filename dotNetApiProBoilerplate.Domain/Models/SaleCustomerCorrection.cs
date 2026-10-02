using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Inventory.Domain.Abstraction;

namespace Inventory.Domain.Entities;

public sealed class SaleCustomerCorrection : TenantEntity
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public Guid? PreviousCustomerId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ActorUserId { get; set; }
    [MaxLength(500)] public string Reason { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal TransferredDebt { get; set; }
}
