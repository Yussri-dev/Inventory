using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Sales.Requests
{
    public class CreatePendingSaleRequest
    {
        public Guid? CustomerId { get; set; }
        public DateTime? SaleDate { get; set; }
        public List<SaleLineItem> SaleLines { get; set; } = new();
    }
}
