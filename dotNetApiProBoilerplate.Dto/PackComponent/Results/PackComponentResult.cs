using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.PackComponent.Results
{
    public class PackComponentResult
    {
        public Guid Id { get; set; }
        public Guid ComponentCatalogId { get; set; }

        public string ComponentName { get; set; } = string.Empty;
        public string? ComponentBarCode { get; set; }

        public decimal Quantity { get; set; }


    }
}
