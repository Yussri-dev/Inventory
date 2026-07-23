using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalReturnableSaleResult
    {
        public Guid LocalSaleId { get; init; }

        public Guid? ServerSaleId { get; init; }

        public string LocalInvoiceNumber { get; init; } = string.Empty;

        public string? ServerInvoiceNumber { get; init; }

        public DateTime SaleDateUtc { get; init; }

        public Guid? CustomerLocalId { get; init; }

        public Guid? CustomerServerId { get; init; }

        public string? CustomerName { get; init; }

        public decimal TotalAmount { get; init; }

        public string SyncStatus { get; init; } = string.Empty;

        public IReadOnlyList<LocalReturnableSaleLineResult> Lines { get; init; } =
            Array.Empty<LocalReturnableSaleLineResult>();
    }
}
