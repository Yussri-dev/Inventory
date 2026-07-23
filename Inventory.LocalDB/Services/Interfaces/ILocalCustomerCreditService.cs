using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.LocalDB.Services.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalCustomerCreditService
    {
        Task<List<LocalCustomerCreditResult>>
            GetCustomersWithBalanceAsync(
                CancellationToken cancellationToken = default);

        Task<LocalCustomerDetailResult?>
            GetCustomerDetailAsync(
                Guid customerLocalId,
                CancellationToken cancellationToken = default);

        Task<LocalCustomerTransactionResult>
            RegisterPaymentAsync(
                RegisterCustomerPaymentRequest request,
                CancellationToken cancellationToken = default);

        Task<LocalCustomerTransactionResult>
            RegisterRefundAsync(
                RegisterCustomerRefundRequest request,
                CancellationToken cancellationToken = default);

        /*
         * Used by the local Sale complete workflow.
         * No CustomerTransaction outbox item is created because the Sale
         * complete endpoint is authoritative on the server.
         */
        Task RecordSaleCreditAsync(
            Guid customerLocalId,
            Guid saleLocalId,
            Guid? saleServerId,
            decimal amount,
            DateTime transactionDateUtc,
            string? description = null,
            CancellationToken cancellationToken = default);

        /*
         * Used by the local Return complete workflow.
         * No CustomerTransaction outbox item is created because the Return
         * complete endpoint is authoritative on the server.
         */
        Task RecordReturnCreditAsync(
            Guid customerLocalId,
            Guid returnLocalId,
            decimal amount,
            DateTime transactionDateUtc,
            string? description = null,
            CancellationToken cancellationToken = default);
    }
}
