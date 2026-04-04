namespace Inventory.Dto.Queries
{
    public class SaleHistoryQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? Search { get; set; }

        // ← Stocker les valeurs brutes
        private DateTime? _dateFrom;
        private DateTime? _dateTo;

        public DateTime? DateFrom
        {
            get => _dateFrom;
            set => _dateFrom = value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : null;
        }

        public DateTime? DateTo
        {
            get => _dateTo;
            set => _dateTo = value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : null;
        }

        public Guid? CustomerId { get; set; }
        public bool WalkInOnly { get; set; } = false;
        public List<string>? PaymentStatuses { get; set; }
        public List<string>? SaleStatuses { get; set; }
    }
}