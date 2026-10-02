using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Sales.Requests;

public sealed class CorrectSaleCustomerRequest
{
    public Guid OperationId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? ExpectedCustomerId { get; set; }
    public DateTime? ExpectedModifiedAt { get; set; }
    public decimal ExpectedDebt { get; set; }
    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}
