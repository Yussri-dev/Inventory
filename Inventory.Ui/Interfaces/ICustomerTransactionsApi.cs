using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface ICustomerTransactionsApi
    {
        [Post("/api/v1/customertransactions/register-payment")]
        Task<CustomerTransactionResult> RegisterPayment(
            [Body] RegisterCustomerPaymentRequest request
        );

        [Get("/api/v1/customertransactions/search")]
        Task<PagedResult<CustomerTransactionResult>> Search(
            [Query] CustomerTransactionQuery query
        );

        [Get("/api/v1/customertransactions/customers-with-balance")]
        Task<List<CustomerCreditResult>> GetCustomersWithBalance();

        [Post("/api/v1/customertransactions/register-refund")]
        Task<CustomerTransactionResult> RegisterRefund(
            [Body] RegisterCustomerRefundRequest request
        );
    }
}
