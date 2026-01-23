namespace Inventory.Dto.CustomerTransactions.Requests
{
    public class RegisterCustomerPaymentRequest
    {
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public bool IsCash { get; set; } = true;
    }
}
