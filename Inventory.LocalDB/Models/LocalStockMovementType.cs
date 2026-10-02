using Inventory.Dto.Enums;
namespace Inventory.LocalDB.Models
{
    public static class LocalStockMovementType
    {
        public const StockMovementType Sale = StockMovementType.Sale;
        public const StockMovementType Purchase = StockMovementType.Purchase;
        public const StockMovementType Return = StockMovementType.Return;
        public const StockMovementType Adjustment = StockMovementType.Adjustment;
        public const StockMovementType Transfer = StockMovementType.Transfer;
        public const StockMovementType InitialStock = StockMovementType.InitialStock;
        public const StockMovementType Damage = StockMovementType.Damage;
    }
}
