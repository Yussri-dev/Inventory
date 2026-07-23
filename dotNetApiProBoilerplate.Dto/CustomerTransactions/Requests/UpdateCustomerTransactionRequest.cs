
using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.CustomerTransactions.Requests
{
    public sealed class UpdateCustomerTransactionRequest
    {
        public Guid Id { get; set; }
        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
