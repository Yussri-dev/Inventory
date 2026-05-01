using Inventory.Ui.Components.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.State
{
    public class PosState
    {
        public List<CartLine> Lines { get; set; } = new();

        public decimal TotalAmount => Lines.Sum(l => l.Amount);
        public decimal TotalQuantity => Lines.Sum(l => l.Quantity);

        public event Action? OnChange;

        public void Notify() => OnChange?.Invoke();
    }
}
