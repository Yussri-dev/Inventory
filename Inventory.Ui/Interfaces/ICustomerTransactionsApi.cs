using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface ICustomerTransactionsApi
    {
        [Post(
            "/api/v1.0/customertransactions/register-payment")]
        Task<CustomerTransactionResult> RegisterPayment(
            [Body] RegisterCustomerPaymentRequest request,
            CancellationToken cancellationToken = default);

        [Post(
            "/api/v1.0/customertransactions/register-refund")]
        Task<CustomerTransactionResult> RegisterRefund(
            [Body] RegisterCustomerRefundRequest request,
            CancellationToken cancellationToken = default);

        [Get(
            "/api/v1.0/customertransactions/{id}")]
        Task<CustomerTransactionResult> GetById(
            Guid id,
            CancellationToken cancellationToken = default);

        [Get(
            "/api/v1.0/customertransactions/customers-with-balance")]
        Task<List<CustomerCreditResult>>
            GetCustomersWithBalance(
                CancellationToken cancellationToken = default);

        [Get(
            "/api/v1.0/customertransactions/customer-detail/{customerId}")]
        Task<CustomerDetailResult> GetCustomerDetail(
            Guid customerId,
            CancellationToken cancellationToken = default);
    }
}