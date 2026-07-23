using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalPurchaseLineResult
    {
        public Guid Id { get; set; }

        public Guid ProductLocalId { get; set; }

        public Guid? ProductServerId { get; set; }

        public string ProductName { get; set; } =
            string.Empty;

        public string? ProductBarcode { get; set; }

        public decimal QuantityOrdered { get; set; }

        public decimal QuantityReceived { get; set; }

        public decimal UnitPurchasePrice { get; set; }

        public decimal VatRate { get; set; }

        public decimal LineAmountExclVat { get; set; }

        public decimal VatAmount { get; set; }

        public decimal LineAmountInclVat { get; set; }
    }
}
