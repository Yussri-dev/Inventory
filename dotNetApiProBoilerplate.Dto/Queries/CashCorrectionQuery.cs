using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Queries
{
    public class CashCorrectionQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string? Search { get; init; }
        public string SortBy { get; init; } = "CreatedAt";
        public bool Desc { get; init; } = true;
    }
}
