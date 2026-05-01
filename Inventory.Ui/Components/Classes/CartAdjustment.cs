using Inventory.Dto.Enums;

namespace Inventory.Ui.Components.Classes
{
    public class CartAdjustment
    {
        public AdjustmentType Type { get; set; }
        public decimal Value { get; set; }
        public string? Note { get; set; }

        public decimal Apply(decimal basePrice)
        {
            var result = Type switch
            {
                AdjustmentType.PriceOverride => Value,
                AdjustmentType.DiscountPercent => basePrice * (1 - Value / 100),
                AdjustmentType.DiscountAmount => basePrice - Value,
                AdjustmentType.Fee => basePrice + Value,
                _ => basePrice
            };

            return Math.Max(0, result);
        }

        public string Display =>
        Type switch
        {
            AdjustmentType.PriceOverride => $"Prix modifié → {Value:0.00} €",
            AdjustmentType.DiscountPercent => $"Remise → -{Value:0.##} %",
            AdjustmentType.DiscountAmount => $"Remise → -{Value:0.00} €",
            AdjustmentType.Fee => $"Frais → +{Value:0.00} €",
            AdjustmentType.Note => $"Note → {Note}",
            _ => ""
        };
    }
}
