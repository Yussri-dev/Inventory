using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalSalesHistoryQuery
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 25;

        public DateTime? DateFromUtc { get; set; }

        /*
         * Exclusive upper bound. For example, selecting 17 July produces
         * 18 July 00:00 local converted to UTC.
         */
        public DateTime? DateToExclusiveUtc { get; set; }

        public string? InvoiceSearch { get; set; }

        public Guid? CustomerLocalId { get; set; }

        public bool WalkInOnly { get; set; }

        public IReadOnlyCollection<string>? PaymentStatuses { get; set; }

        public IReadOnlyCollection<string>? SaleStatuses { get; set; }
    }
}
