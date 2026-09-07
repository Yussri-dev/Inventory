using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;

namespace Inventory.Services.Handlers
{
    public sealed class SaleSyncOperationHandler
        : SyncOperationHandlerBase
    {
        private const string CreateOperation = "Create";

        private readonly SaleService _saleService;

        public SaleSyncOperationHandler(
            SaleService saleService)
        {
            ArgumentNullException.ThrowIfNull(
                saleService);

            _saleService =
                saleService;
        }

        public override string EntityName => "Sale";

        protected override async Task<SyncBatchItemResult>
            ProcessCoreAsync(
                SyncBatchOperationRequest operation,
                CancellationToken cancellationToken)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            if (!IsOperation(
                    operation,
                    CreateOperation))
            {
                return Conflict(
                    operation,
                    $"Unsupported Sale operation " +
                    $"'{operation.Operation}'. Only Create is supported.");
            }

            var request =
                DeserializePayload<CreateCompleteSaleRequest>(
                    operation);

            /*
             * The synchronization envelope contains the official
             * idempotency identifier.
             */
            request.ClientOperationId =
                operation.ClientOperationId;

            /*
             * LocalEntityId is a SQLite identifier. It must never be
             * interpreted as an existing PostgreSQL pending Sale.
             */
            request.PendingSaleId =
                null;

            if (!request.CashSessionId.HasValue ||
                request.CashSessionId.Value == Guid.Empty)
            {
                return Conflict(
                    operation,
                    "CashSessionId is required for offline Sale " +
                    "synchronization.");
            }

            if (request.Lines == null ||
                request.Lines.Count == 0)
            {
                return Conflict(
                    operation,
                    "The offline Sale contains no lines.");
            }

            if (request.Payments == null ||
                request.Payments.Count == 0)
            {
                return Conflict(
                    operation,
                    "The offline Sale contains no payments.");
            }

            var invalidLine =
                request.Lines.FirstOrDefault(
                    line =>
                        line.ProductId == Guid.Empty ||
                        line.Quantity <= 0m ||
                        line.UnitPrice < 0m ||
                        line.VatRate < 0m ||
                        line.VatRate > 100m ||
                        line.DiscountPercent < 0m ||
                        line.DiscountPercent > 100m);

            if (invalidLine != null)
            {
                return Conflict(
                    operation,
                    "The offline Sale contains an invalid line.");
            }

            var invalidPayment =
                request.Payments.FirstOrDefault(
                    payment =>
                        payment.Amount <= 0m ||
                        string.IsNullOrWhiteSpace(
                            payment.PaymentMethod));

            if (invalidPayment != null)
            {
                return Conflict(
                    operation,
                    "The offline Sale contains an invalid payment.");
            }

            /*
             * SyncOperationExecutor already owns the PostgreSQL
             * transaction. Every SaveChangesAsync executed by
             * SaleService remains inside that transaction.
             */
            var sale =
                await _saleService
                    .CreateCompleteAsync(
                        request);

            cancellationToken
                .ThrowIfCancellationRequested();

            if (sale == null || sale.Id == Guid.Empty)
            {
                return Failed(
                    operation,
                    "SaleService returned no valid server Sale identifier.");
            }

            if (string.IsNullOrWhiteSpace(
                    sale.InvoiceNumber))
            {
                return Failed(
                    operation,
                    "SaleService returned no server invoice number.");
            }

            var result =
                Done(
                    operation,
                    sale.Id);

            result.ServerReferenceNumber =
                sale.InvoiceNumber.Trim();

            return result;
        }
    }
}