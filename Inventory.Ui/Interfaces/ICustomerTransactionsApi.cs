using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface ICustomerTransactionsApi
    {
        [Post("/api/v1.0/customer-transactions/payment")]
        Task<CustomerTransactionResult> RegisterPayment(
            [Body] RegisterCustomerPaymentRequest request,
            CancellationToken cancellationToken = default);

        [Post("/api/v1.0/customer-transactions/refund")]
        Task<CustomerTransactionResult> RegisterRefund(
            [Body] RegisterCustomerRefundRequest request,
            CancellationToken cancellationToken = default);

        [Get("/api/v1.0/customer-transactions/{id}")]
        Task<CustomerTransactionResult> GetById(
            Guid id,
            CancellationToken cancellationToken = default);

        [Get("/api/v1.0/customer-transactions/customers-with-balance")]
        Task<List<CustomerCreditResult>> GetCustomersWithBalance(
            CancellationToken cancellationToken = default);

        [Get("/api/v1.0/customer-transactions/customer-detail/{customerId}")]
        Task<CustomerDetailResult> GetCustomerDetail(
            Guid customerId,
            CancellationToken cancellationToken = default);
    }
}
