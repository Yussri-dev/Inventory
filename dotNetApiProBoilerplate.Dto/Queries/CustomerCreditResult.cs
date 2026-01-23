namespace Inventory.Dto.Queries
{
    public class CustomerCreditResult
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Balance { get; set; }
    }

}
