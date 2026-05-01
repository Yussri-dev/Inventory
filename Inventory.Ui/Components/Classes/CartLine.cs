using Inventory.Dto.Enums;

namespace Inventory.Ui.Components.Classes
{
    public class CartLine
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal BaseSalePrice { get; set; } // TTC
        public decimal TaxRate { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsPack { get; set; }
        public decimal PackSize { get; set; } = 1m;
        public List<CartAdjustment> Adjustments { get; set; } = new();

        public decimal EffectiveUnitPrice
        {
            get
            {
                decimal price = BaseSalePrice;
                foreach (var adj in Adjustments)
                    price = adj.Apply(price);
                return price;
            }
        }

        public decimal Amount => Quantity * EffectiveUnitPrice; // TTC

        public decimal TaxAmount
        {
            get
            {
                var divisor = 1m + (TaxRate / 100m);
                if (divisor <= 0) return 0m;

                var ht = Amount / divisor;
                return Amount - ht;
            }
        }

        public decimal AmountExclVat => Amount - TaxAmount;

        public decimal CalculateDiscountPercent()
        {
            var discountAdj = Adjustments.FirstOrDefault(a => a.Type == AdjustmentType.DiscountPercent);
            if (discountAdj != null)
                return discountAdj.Value;

            var discountAmount = Adjustments.FirstOrDefault(a => a.Type == AdjustmentType.DiscountAmount);
            if (discountAmount != null && BaseSalePrice > 0)
                return (discountAmount.Value / BaseSalePrice) * 100;

            return 0;
        }
    }
}
