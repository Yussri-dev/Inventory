using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Models
{
    public class LocalCustomerTransaction : ILocalTenantEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }

        public Guid? ServerId { get; set; }

        public Guid ClientOperationId { get; set; } = Guid.NewGuid();

        public Guid CustomerLocalId { get; set; }

        public Guid? CustomerServerId { get; set; }

        public Guid? LocalCashSessionId { get; set; }

        public Guid? ServerCashSessionId { get; set; }

        public Guid? SaleLocalId { get; set; }

        public Guid? SaleServerId { get; set; }

        public Guid? ReturnLocalId { get; set; }

        public Guid? ReturnServerId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Origin { get; set; } =
            LocalCustomerTransactionOrigin.Manual;

        /*
         * True only for standalone payment/refund actions.
         * Sale/return ledger rows are uploaded by their authoritative
         * Sale/Return complete endpoints and must not be sent twice.
         */
        public bool UploadRequired { get; set; } = true;

        public bool IsCash { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceBefore { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime TransactionDateUtc { get; set; } =
            DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string SyncStatus { get; set; } =
            SyncQueueStatus.Pending;

        public DateTime CreatedAtUtc { get; set; } =
            DateTime.UtcNow;

        public DateTime? ModifiedAtUtc { get; set; }

        public DateTime? LastSyncedAtUtc { get; set; }

        [ForeignKey(nameof(CustomerLocalId))]
        public LocalCustomer Customer { get; set; } = null!;
    }
}
