using Inventory.Dto.Enums;

namespace Inventory.Services.Exceptions
{
    public static class CashMovementTypeExtensions
    {
        public static bool IsIn(this CashMovementType type) =>
            type is CashMovementType.Opening
                 or CashMovementType.Sale
                 or CashMovementType.Deposit;

        public static bool IsOut(this CashMovementType type) =>
            type is CashMovementType.Withdrawal
                 or CashMovementType.Refund
                 or CashMovementType.Closing;
    }

}
