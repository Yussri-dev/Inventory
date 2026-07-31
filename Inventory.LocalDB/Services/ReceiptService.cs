using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Inventory.LocalDB.Services
{
    public sealed class ReceiptService : IReceiptService
    {
        private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,

            WriteIndented =
                false
        };

        /*
         * Empêche deux impressions simultanées du même ticket.
         *
         * Exemple :
         * double clic sur "Print".
         */
        private static readonly ConcurrentDictionary<Guid, SemaphoreSlim>
            ReceiptLocks =
                new();

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;
        private readonly IReceiptPrinter _receiptPrinter;
        private readonly IReceiptPdfGenerator _pdfGenerator;
        private readonly ReceiptSettings _settings;
        private readonly ILogger<ReceiptService> _logger;
        private readonly ILocalStoreProfileService _storeProfileService;
        private readonly ReceiptPrinterOptions _printerOptions;
        public ReceiptService(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext,
            IReceiptPrinter receiptPrinter,
            IReceiptPdfGenerator pdfGenerator,
            ILocalStoreProfileService storeProfileService,
            IOptions<ReceiptSettings> settings,
            IOptions<ReceiptPrinterOptions> printerOptions,
            ILogger<ReceiptService> logger)
        {
            _db = db;
            _tenantContext = tenantContext;
            _receiptPrinter = receiptPrinter;
            _printerOptions = printerOptions.Value;
            _storeProfileService = storeProfileService;
            _pdfGenerator = pdfGenerator;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<LocalReceipt> CreateReceiptAsync(Guid localSaleId, CancellationToken cancellationToken = default)
        {
            if (localSaleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The local sale id is required.",
                    nameof(localSaleId));
            }

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                cancellationToken);

            try
            {
                _db.ChangeTracker.Clear();

                /*
                 * Une vente possède un seul snapshot de ticket.
                 */
                var existingReceipt =
                    await _db.Receipts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            receipt =>
                                receipt.TenantId == tenantId &&
                                receipt.LocalSaleId == localSaleId,
                            cancellationToken);

                if (existingReceipt != null)
                {
                    return existingReceipt;
                }

                var sale =
                    await _db.Sales
                        .AsNoTracking()
                        .Include(item =>
                            item.Lines)
                        .Include(item =>
                            item.Payments)
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId &&
                                item.Id == localSaleId,
                            cancellationToken);

                if (sale == null)
                {
                    throw new InvalidOperationException(
                        $"Local sale '{localSaleId}' was not found.");
                }

                if (!string.Equals(
                        sale.Status,
                        LocalSaleStatus.Completed,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Receipt creation is only allowed for a completed sale. " +
                        $"Current status: {sale.Status}.");
                }

                if (sale.Lines.Count == 0)
                {
                    throw new InvalidOperationException(
                        "A receipt cannot be created for a sale without lines.");
                }

                var customerName =
                    await ResolveCustomerNameAsync(
                        sale,
                        tenantId,
                        cancellationToken);

                var store = await _storeProfileService.GetCurrentAsync(cancellationToken);

                if (store == null)
                {
                    throw new InvalidOperationException(
                        "The local store information is missing. " +
                        "Synchronize the application before creating a receipt.");
                }
                var snapshot =
                BuildSnapshot(
                    sale,
                    customerName,
                    store);

                var snapshotJson =
                    JsonSerializer.Serialize(
                        snapshot,
                        JsonOptions);

                var receipt =
                    new LocalReceipt
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        LocalSaleId =
                            sale.Id,

                        ServerSaleId =
                            sale.ServerId,

                        InvoiceNumber =
                            sale.LocalInvoiceNumber,

                        SnapshotJson =
                            snapshotJson,

                        SnapshotHash =
                            ComputeSnapshotHash(
                                snapshotJson),

                        CreatedAtUtc =
                            DateTime.UtcNow,

                        SyncStatus =
                            SyncQueueStatus.Pending
                    };

                _db.Receipts.Add(
                    receipt);

                try
                {
                    await _db.SaveChangesAsync(
                        cancellationToken);
                }
                catch (DbUpdateException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Concurrent receipt creation detected for sale {SaleId}.",
                        localSaleId);

                    _db.ChangeTracker.Clear();

                    var concurrentReceipt =
                        await _db.Receipts
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                item =>
                                    item.TenantId == tenantId &&
                                    item.LocalSaleId == localSaleId,
                                cancellationToken);

                    if (concurrentReceipt != null)
                    {
                        return concurrentReceipt;
                    }

                    throw;
                }

                _db.Entry(receipt).State =
                    EntityState.Detached;

                _logger.LogInformation(
                    "Local receipt {ReceiptId} created for sale {InvoiceNumber}.",
                    receipt.Id,
                    receipt.InvoiceNumber);

                return receipt;
            }
            finally
            {
                _db.ChangeTracker.Clear();

                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        public Task<LocalReceiptPrintResult> PrintOriginalAsync(
            Guid receiptId,
            CancellationToken cancellationToken = default)
        {
            return PrintInternalAsync(
                receiptId,
                duplicateRequested: false,
                reason: null,
                cancellationToken);
        }

        public Task<LocalReceiptPrintResult> PrintDuplicateAsync(
            Guid receiptId,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            return PrintInternalAsync(
                receiptId,
                duplicateRequested: true,
                reason,
                cancellationToken);
        }

        public async Task<byte[]> GeneratePdfAsync(Guid receiptId, bool duplicate, CancellationToken cancellationToken = default)
        {
            if (receiptId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The receipt id is required.",
                    nameof(receiptId));
            }

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                cancellationToken);

            try
            {
                _db.ChangeTracker.Clear();

                var receipt =
                    await _db.Receipts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId &&
                                item.Id == receiptId,
                            cancellationToken);

                if (receipt == null)
                {
                    throw new InvalidOperationException(
                        $"Receipt '{receiptId}' was not found.");
                }

                var snapshot =
                    DeserializeAndValidateSnapshot(
                        receipt);

                var successfulPrintCount =
                    await _db.ReceiptPrintLogs
                        .AsNoTracking()
                        .CountAsync(
                            log =>
                                log.TenantId == tenantId &&
                                log.LocalReceiptId == receiptId &&
                                log.WasSuccessful,
                            cancellationToken);

                var copyNumber =
                    duplicate
                        ? Math.Max(
                            2,
                            successfulPrintCount + 1)
                        : 1;

                var document =
                    new ReceiptPrintDocument
                    {
                        ReceiptId =
                            receipt.Id,

                        Snapshot =
                            snapshot,

                        IsDuplicate =
                            duplicate,

                        CopyNumber =
                            copyNumber,

                        PrintedAtUtc =
                            DateTime.UtcNow,

                        Reason =
                            duplicate
                                ? "PDF duplicate"
                                : null
                    };

                /*
                 * Générer le PDF en dehors du tracker EF.
                 */
                return await _pdfGenerator.GenerateAsync(
                    document,
                    cancellationToken);
            }
            finally
            {
                _db.ChangeTracker.Clear();

                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        private async Task<LocalReceiptPrintResult> PrintInternalAsync(
            Guid receiptId,
            bool duplicateRequested,
            string? reason,
            CancellationToken cancellationToken)
        {
            if (receiptId == Guid.Empty)
            {
                return LocalReceiptPrintResult.Failed(
                    "The receipt id is required.");
            }

            if (!_printerOptions.Enabled)
            {
                return LocalReceiptPrintResult.Failed(
                    "Physical receipt printing is currently disabled. " +
                    "Use PDF generation until a receipt printer is configured.");
            }

            var receiptLock =
                ReceiptLocks.GetOrAdd(
                    receiptId,
                    _ => new SemaphoreSlim(1, 1));

            await receiptLock.WaitAsync(
                cancellationToken);

            try
            {
                var tenantId =
                    _tenantContext.GetRequiredTenantId();

                await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                    cancellationToken);

                try
                {
                    _db.ChangeTracker.Clear();

                    var receipt =
                        await _db.Receipts
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                item =>
                                    item.TenantId == tenantId &&
                                    item.Id == receiptId,
                                cancellationToken);

                    if (receipt == null)
                    {
                        return LocalReceiptPrintResult.Failed(
                            $"Receipt '{receiptId}' was not found.");
                    }

                    var snapshot =
                        DeserializeAndValidateSnapshot(
                            receipt);

                    var successfulPrintCount =
                        await _db.ReceiptPrintLogs
                            .AsNoTracking()
                            .CountAsync(
                                log =>
                                    log.TenantId == tenantId &&
                                    log.LocalReceiptId == receiptId &&
                                    log.WasSuccessful,
                                cancellationToken);

                    /*
                     * Une deuxième impression ne doit jamais être appelée
                     * Original.
                     */
                    if (!duplicateRequested &&
                        successfulPrintCount > 0)
                    {
                        return LocalReceiptPrintResult.Failed(
                            "The original receipt has already been printed. " +
                            "Use duplicate printing instead.",
                            isDuplicate: false,
                            copyNumber: 1);
                    }

                    /*
                     * Un duplicata est uniquement possible après une
                     * première impression originale réussie.
                     */
                    if (duplicateRequested &&
                        successfulPrintCount == 0)
                    {
                        return LocalReceiptPrintResult.Failed(
                            "No successful original print was found. " +
                            "Print the original receipt first.",
                            isDuplicate: true,
                            copyNumber: 2);
                    }

                    var isDuplicate =
                        duplicateRequested;

                    var copyNumber =
                        isDuplicate
                            ? successfulPrintCount + 1
                            : 1;

                    var printedAtUtc =
                        DateTime.UtcNow;

                    var document =
                        new ReceiptPrintDocument
                        {
                            ReceiptId =
                                receipt.Id,

                            Snapshot =
                                snapshot,

                            IsDuplicate =
                                isDuplicate,

                            CopyNumber =
                                copyNumber,

                            PrintedAtUtc =
                                printedAtUtc,

                            Reason =
                                NormalizeReason(
                                    reason)
                        };

                    var printLog =
                        new LocalReceiptPrintLog
                        {
                            Id = Guid.NewGuid(),

                            TenantId = tenantId,

                            LocalReceiptId = receipt.Id,

                            PrintType = isDuplicate ? ReceiptPrintType.Duplicate : ReceiptPrintType.Original,

                            CopyNumber = copyNumber,

                            PrintedAtUtc = printedAtUtc,

                            /*
                             * Remplace Guid.Empty par ton CurrentUserId
                             * lorsqu'un ILocalCurrentUserContext sera disponible.
                             */
                            PrintedByUserId = Guid.Empty,

                            DeviceName = _receiptPrinter.DeviceName,

                            Reason = NormalizeReason(reason),

                            WasSuccessful = false,

                            ErrorMessage = null
                        };

                    try
                    {
                        await _receiptPrinter.PrintAsync(
                            document,
                            cancellationToken);

                        printLog.WasSuccessful =
                            true;

                        printLog.ErrorMessage =
                            null;

                        _db.ReceiptPrintLogs.Add(
                            printLog);

                        await _db.SaveChangesAsync(
                            cancellationToken);

                        _logger.LogInformation(
                            "{PrintType} receipt {InvoiceNumber}, copy {CopyNumber}, " +
                            "printed successfully.",
                            printLog.PrintType,
                            snapshot.InvoiceNumber,
                            copyNumber);

                        return LocalReceiptPrintResult.Succeeded(
                            printLog.Id,
                            isDuplicate,
                            copyNumber);
                    }
                    catch (OperationCanceledException)
                    {
                        printLog.WasSuccessful =
                            false;

                        printLog.ErrorMessage =
                            "Printing was cancelled.";

                        await SaveFailedPrintLogAsync(
                            printLog);

                        throw;
                    }
                    catch (Exception exception)
                    {
                        printLog.WasSuccessful =
                            false;

                        printLog.ErrorMessage =
                            exception
                                .GetBaseException()
                                .Message;

                        await SaveFailedPrintLogAsync(
                            printLog);

                        _logger.LogError(
                            exception,
                            "Receipt printing failed. ReceiptId={ReceiptId}, " +
                            "CopyNumber={CopyNumber}.",
                            receiptId,
                            copyNumber);

                        return LocalReceiptPrintResult.Failed(
                            printLog.ErrorMessage,
                            isDuplicate,
                            copyNumber,
                            printLog.Id);
                    }
                }
                finally
                {
                    _db.ChangeTracker.Clear();

                    LocalDatabaseWriteGate.Semaphore.Release();
                }
            }
            finally
            {
                receiptLock.Release();
            }
        }

        private async Task SaveFailedPrintLogAsync(
     LocalReceiptPrintLog printLog,
     CancellationToken cancellationToken = default)
        {
            try
            {
                _db.ReceiptPrintLogs.Add(
                    printLog);

                await _db.SaveChangesAsync(
                    cancellationToken);

                _db.Entry(printLog).State =
                    EntityState.Detached;
            }
            catch (Exception exception)
            {
                _db.ChangeTracker.Clear();

                _logger.LogError(
                    exception,
                    "Failed to save unsuccessful receipt print log " +
                    "{PrintLogId}.",
                    printLog.Id);
            }
        }

        private ReceiptSnapshot BuildSnapshot(
    LocalSale sale,
    string customerName,
    LocalStoreProfile store)
        {
            ArgumentNullException.ThrowIfNull(
                sale);

            ArgumentNullException.ThrowIfNull(
                store);

            var lineSnapshots =
                sale.Lines
                    .Select(
                        BuildLineSnapshot)
                    .ToList();

            var subtotalExclVat =
                RoundMoney(
                    lineSnapshots.Sum(
                        line =>
                            line.AmountExclVat));

            var totalVat =
                RoundMoney(
                    lineSnapshots.Sum(
                        line =>
                            line.VatAmount));

            var totalAmount =
                RoundMoney(
                    lineSnapshots.Sum(
                        line =>
                            line.TotalInclVat));

            var payments =
                sale.Payments
                    .Select(payment =>
                        new ReceiptPaymentSnapshot
                        {
                            Method =
                                payment.Method,

                            Amount =
                                RoundMoney(
                                    payment.Amount),

                            TransactionReference =
                                payment.TransactionRef,

                            PaidAtUtc =
                                EnsureUtc(
                                    payment.PaidAtUtc)
                        })
                    .ToList();

            var totalReceived =
                RoundMoney(
                    payments.Sum(
                        payment =>
                            payment.Amount));

            var changeAmount =
                RoundMoney(
                    Math.Max(
                        0m,
                        totalReceived -
                        totalAmount));

            var vatSummary =
                lineSnapshots
                    .GroupBy(
                        line =>
                            line.VatRate)
                    .Select(group =>
                        new ReceiptVatSnapshot
                        {
                            VatRate =
                                group.Key,

                            AmountExclVat =
                                RoundMoney(
                                    group.Sum(
                                        line =>
                                            line.AmountExclVat)),

                            VatAmount =
                                RoundMoney(
                                    group.Sum(
                                        line =>
                                            line.VatAmount)),

                            AmountInclVat =
                                RoundMoney(
                                    group.Sum(
                                        line =>
                                            line.TotalInclVat))
                        })
                    .OrderBy(
                        summary =>
                            summary.VatRate)
                    .ToList();

            /*
             * Nom commercial du tenant.
             */
            var companyName =
                !string.IsNullOrWhiteSpace(
                    store.TradeName)
                        ? store.TradeName.Trim()
                        : !string.IsNullOrWhiteSpace(
                            store.Name)
                                ? store.Name.Trim()
                                : "MAGASIN";

            var companyAddress =
                BuildStoreAddress(
                    store);

            var companyPhone =
                !string.IsNullOrWhiteSpace(
                    store.Phone)
                        ? store.Phone.Trim()
                        : NormalizeOptionalText(
                            store.Mobile);

            var receiptBarcodeValue =
                NormalizeOptionalText(
                    sale.ReceiptBarcodeValue);

            if (string.IsNullOrWhiteSpace(
                    receiptBarcodeValue))
            {
                throw new InvalidOperationException(
                    $"Sale '{sale.LocalInvoiceNumber}' does not have " +
                    "a receipt barcode value.");
            }

            /*
             * Configuration propre au tenant.
             *
             * ReceiptSettings sert uniquement de valeur de secours.
             */
            var currencyCode =
                FirstNotEmpty(
                    store.ReceiptCurrencyCode,
                    store.Currency,
                    _settings.CurrencyCode,
                    "EUR")
                .ToUpperInvariant();

            var cashierName =
                FirstNotEmpty(
                    store.ReceiptDefaultCashierName,
                    _settings.DefaultCashierName,
                    "POS");

            var headerTagLine =
                FirstOptionalText(
                    store.ReceiptHeaderTagLine,
                    _settings.HeaderTagLine);

            var socialLine =
                FirstOptionalText(
                    store.ReceiptSocialLine,
                    store.Website,
                    _settings.SocialLine);

            var extraAddressLine =
                FirstOptionalText(
                    store.ReceiptExtraAddressLine,
                    _settings.ExtraAddressLine);

            var footerText =
                FirstOptionalText(
                    store.ReceiptFooter,
                    _settings.FooterText);

            var logoBytes =
                store.ReceiptLogoBytes is { Length: > 0 }
                    ? store.ReceiptLogoBytes.ToArray()
                    : null;

            return new ReceiptSnapshot
            {
                CompanyName =
                    companyName,

                CompanyAddress =
                    companyAddress,

                ExtraAddressLine =
                    extraAddressLine,

                CompanyPhone =
                    companyPhone,

                CompanyEmail =
                    NormalizeOptionalText(
                        store.Email),

                CompanyTaxNumber =
                    NormalizeOptionalText(
                        store.TaxNumber),

                CompanyLegalName =
                    NormalizeOptionalText(
                        store.LegalName),

                CompanyRegistrationNumber =
                    NormalizeOptionalText(
                        store.RegistrationNumber),

                CompanyMobile =
                    NormalizeOptionalText(
                        store.Mobile),

                CompanyWebsite =
                    NormalizeOptionalText(
                        store.Website),

                ReceiptHeader =
                    NormalizeOptionalText(
                        store.ReceiptHeader),

                HeaderTagLine =
                    headerTagLine,

                SocialLine =
                    socialLine,

                LogoBytes =
                    logoBytes,

                InvoiceNumber =
                    sale.LocalInvoiceNumber,

                BarcodeValue =
                    receiptBarcodeValue,

                SaleDateUtc =
                    EnsureUtc(
                        sale.SaleDateUtc),

                CashierName =
                    cashierName,

                CustomerName =
                    string.IsNullOrWhiteSpace(
                        customerName)
                            ? "CLIENT PASSAGER"
                            : customerName.Trim(),

                Lines =
                    lineSnapshots,

                SubtotalExclVat =
                    subtotalExclVat,

                TotalVat =
                    totalVat,

                TotalAmount =
                    totalAmount,

                TotalReceived =
                    totalReceived,

                ChangeAmount =
                    changeAmount,

                CurrencyCode =
                    currencyCode,

                VatSummary =
                    vatSummary,

                Payments =
                    payments,

                FooterText =
                    footerText
            };
        }

        private static string FirstNotEmpty(
    params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(
                        value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        private static string? FirstOptionalText(
            params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(
                        value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        private static string? BuildStoreAddress(LocalStoreProfile store)
        {
            var cityLine =
                string.Join(
                    " ",
                    new[]
                    {
                NormalizeOptionalText(
                    store.PostalCode),

                NormalizeOptionalText(
                    store.City)
                    }
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value)));

            var addressParts =
                new[]
                {
            NormalizeOptionalText(
                store.Address),

            NormalizeOptionalText(
                cityLine),

            NormalizeOptionalText(
                store.State),

            NormalizeOptionalText(
                store.Country)
                }
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .ToList();

            return addressParts.Count == 0
                ? null
                : string.Join(
                    Environment.NewLine,
                    addressParts);
        }

        private static string? NormalizeOptionalText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static ReceiptLineSnapshot BuildLineSnapshot(
            LocalSaleLine line)
        {
            var quantity =
                RoundQuantity(
                    line.Quantity);

            var unitPrice =
                RoundMoney(
                    line.UnitPrice);

            var grossAmount =
                RoundMoney(
                    quantity *
                    unitPrice);

            var percentageDiscount =
                RoundMoney(
                    grossAmount *
                    line.DiscountPercent /
                    100m);

            /*
             * Dans LocalSaleLine, DiscountAmount représente la réduction
             * fixe de toute la ligne.
             */
            var totalDiscount =
                RoundMoney(
                    Math.Clamp(
                        percentageDiscount +
                        line.DiscountAmount,
                        0m,
                        grossAmount));

            var totalInclVat =
                RoundMoney(
                    grossAmount -
                    totalDiscount);

            var vatDivisor =
                1m +
                line.VatRate /
                100m;

            var amountExclVat =
                vatDivisor > 0m
                    ? RoundMoney(
                        totalInclVat /
                        vatDivisor)
                    : totalInclVat;

            var vatAmount =
                RoundMoney(
                    totalInclVat -
                    amountExclVat);

            return new ReceiptLineSnapshot
            {
                ProductName =
                    line.ProductName,

                Barcode =
                    line.ProductBarcode,

                Quantity =
                    quantity,

                UnitPrice =
                    unitPrice,

                DiscountPercent =
                    RoundPercentage(
                        line.DiscountPercent),

                DiscountAmount =
                    RoundMoney(
                        line.DiscountAmount),

                VatRate =
                    RoundPercentage(
                        line.VatRate),

                GrossAmountInclVat =
                    grossAmount,

                TotalDiscount =
                    totalDiscount,

                AmountExclVat =
                    amountExclVat,

                VatAmount =
                    vatAmount,

                TotalInclVat =
                    totalInclVat
            };
        }

        private async Task<string> ResolveCustomerNameAsync(
    LocalSale sale,
    Guid tenantId,
    CancellationToken cancellationToken)
        {
            if (!sale.CustomerLocalId.HasValue ||
                sale.CustomerLocalId.Value == Guid.Empty)
            {
                return "Walk-in customer";
            }

            var customerName =
                await _db.Customers
                    .AsNoTracking()
                    .Where(customer =>
                        customer.TenantId == tenantId &&
                        customer.Id == sale.CustomerLocalId.Value)
                    .Select(customer =>
                        customer.Name)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            return string.IsNullOrWhiteSpace(
                customerName)
                    ? "Walk-in customer"
                    : customerName;
        }

        private static ReceiptSnapshot DeserializeAndValidateSnapshot(
            LocalReceipt receipt)
        {
            if (string.IsNullOrWhiteSpace(
                    receipt.SnapshotJson))
            {
                throw new InvalidOperationException(
                    "The receipt snapshot is empty.");
            }

            if (!string.IsNullOrWhiteSpace(
                    receipt.SnapshotHash))
            {
                var currentHash =
                    ComputeSnapshotHash(
                        receipt.SnapshotJson);

                if (!string.Equals(
                        currentHash,
                        receipt.SnapshotHash,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The receipt snapshot failed its integrity check.");
                }
            }

            var snapshot =
                JsonSerializer.Deserialize<ReceiptSnapshot>(
                    receipt.SnapshotJson,
                    JsonOptions);

            return snapshot ??
                   throw new InvalidOperationException(
                       "The receipt snapshot could not be deserialized.");
        }

        private static string ComputeSnapshotHash(
            string snapshotJson)
        {
            var bytes =
                Encoding.UTF8.GetBytes(
                    snapshotJson);

            var hash =
                SHA256.HashData(
                    bytes);

            return Convert.ToHexString(
                hash);
        }

        private static string? NormalizeReason(
            string? reason)
        {
            if (string.IsNullOrWhiteSpace(
                    reason))
            {
                return null;
            }

            var normalized =
                reason.Trim();

            return normalized.Length <= 250
                ? normalized
                : normalized[..250];
        }

        private static DateTime EnsureUtc(
            DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc =>
                    value,

                DateTimeKind.Local =>
                    value.ToUniversalTime(),

                _ =>
                    DateTime.SpecifyKind(
                        value,
                        DateTimeKind.Utc)
            };
        }

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static decimal RoundQuantity(
            decimal value)
        {
            return Math.Round(
                value,
                3,
                MidpointRounding.AwayFromZero);
        }

        private static decimal RoundPercentage(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }
    }
}