using Inventory.Dto.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.CashSessions.Results
{
    public class CashSessionResult
    {
        public Guid Id { get; set; }

        public string SessionNumber { get; set; } = null!;

        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public decimal OpeningAmount { get; set; }

        public decimal ClosingAmountExpected { get; set; }

        public decimal ClosingAmountCounted { get; set; }

        public decimal Difference { get; set; }

        public CashSessionStatus Status { get; set; }

        public Guid OpenedByUserId { get; set; }

        public Guid? ClosedByUserId { get; set; }

        [MaxLength(1000)]
        public string? OpeningNotes { get; set; }

        [MaxLength(1000)]
        public string? ClosingNotes { get; set; }
    }
}
