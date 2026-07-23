using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Models
{
    public sealed class LocalPackComponent
    {
        public Guid ProductCatalogId { get; set; }

        public Guid ComponentCatalogId { get; set; }

        public string ComponentName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public LocalProductCatalog ProductCatalog { get; set; } = null!;
    }
}
