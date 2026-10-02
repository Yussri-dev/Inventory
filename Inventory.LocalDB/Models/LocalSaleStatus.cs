using Inventory.Dto.Enums;
namespace Inventory.LocalDB.Models
{
    public static class LocalSaleStatus
    {
        public const SaleStatus Draft = SaleStatus.Draft;
        public const SaleStatus Completed = SaleStatus.Completed;
        public const SaleStatus Cancelled = SaleStatus.Cancelled;
        public const SaleStatus Refunded = SaleStatus.Refunded;
    }
}