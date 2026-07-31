using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services
{
    public sealed class LocalSalesHistoryService
        : ILocalSalesHistoryService
    {
        private const int MaximumPageSize = 100;
        private const int MaximumCustomerResults = 100;

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public LocalSalesHistoryService(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<LocalSalesHistoryPageResult> SearchAsync(
            LocalSalesHistoryQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            ValidatePaging(
                query.Page,
                query.PageSize);

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            IQueryable<LocalSale> salesQuery =
                _db.Sales
                    .AsNoTracking()
                    .Where(sale =>
                        sale.TenantId == tenantId);

            if (query.DateFromUtc.HasValue)
            {
                var dateFromUtc =
                    EnsureUtc(
                        query.DateFromUtc.Value);

                salesQuery =
                    salesQuery.Where(sale =>
                        sale.SaleDateUtc >= dateFromUtc);
            }

            if (query.DateToExclusiveUtc.HasValue)
            {
                var dateToExclusiveUtc =
                    EnsureUtc(
                        query.DateToExclusiveUtc.Value);

                salesQuery =
                    salesQuery.Where(sale =>
                        sale.SaleDateUtc < dateToExclusiveUtc);
            }

            if (!string.IsNullOrWhiteSpace(
                    query.InvoiceSearch))
            {
                var search =
                    query.InvoiceSearch
                        .Trim()
                        .ToLower();

                /*
                 * ToLower + Contains is translated by the SQLite provider.
                 * Both local and authoritative server invoice numbers are
                 * searchable while offline.
                 */
                salesQuery =
                    salesQuery.Where(sale =>
                        sale.LocalInvoiceNumber
                            .ToLower()
                            .Contains(search) ||
                        (
                            sale.ServerInvoiceNumber != null &&
                            sale.ServerInvoiceNumber
                                .ToLower()
                                .Contains(search)
                        ));
            }

            if (query.WalkInOnly)
            {
                salesQuery =
                    salesQuery.Where(sale =>
                        !sale.CustomerLocalId.HasValue);
            }
            else if (query.CustomerLocalId.HasValue &&
                     query.CustomerLocalId.Value != Guid.Empty)
            {
                var customerLocalId =
                    query.CustomerLocalId.Value;

                salesQuery =
                    salesQuery.Where(sale =>
                        sale.CustomerLocalId ==
                            customerLocalId);
            }

            var paymentStatuses =
                NormalizePaymentStatuses(
                    query.PaymentStatuses);

            if (paymentStatuses.Count > 0)
            {
                salesQuery =
                    salesQuery.Where(sale =>
                        paymentStatuses.Contains(
                            sale.PaymentStatus));
            }

            var saleStatuses =
                NormalizeSaleStatuses(
                    query.SaleStatuses);

            if (saleStatuses.Count > 0)
            {
                salesQuery =
                    salesQuery.Where(sale =>
                        saleStatuses.Contains(
                            sale.Status));
            }

            var totalCount =
                await salesQuery.CountAsync(
                    cancellationToken);

            var rawItems =
                await salesQuery
                    .OrderByDescending(sale =>
                        sale.SaleDateUtc)
                    .ThenByDescending(sale =>
                        sale.CreatedAtUtc)
                    .Skip(
                        (query.Page - 1) *
                        query.PageSize)
                    .Take(
                        query.PageSize)
                    .Select(sale =>
                        new RawSaleHistoryItem
                        {
                            LocalId =
                                sale.Id,

                            ServerId =
                                sale.ServerId,

                            LocalInvoiceNumber =
                                sale.LocalInvoiceNumber,

                            ServerInvoiceNumber =
                                sale.ServerInvoiceNumber,

                            SaleDateUtc =
                                sale.SaleDateUtc,

                            CustomerLocalId =
                                sale.CustomerLocalId,

                            CustomerServerId =
                                sale.CustomerServerId,

                            TotalAmount =
                                sale.TotalAmount,

                            PaidAmount =
                                sale.PaidAmount,

                            ChangeAmount =
                                sale.ChangeAmount,

                            Status =
                                sale.Status,

                            PaymentStatus =
                                sale.PaymentStatus,

                            SyncStatus =
                                sale.SyncStatus
                        })
                    .ToListAsync(
                        cancellationToken);

            var customerIds =
                rawItems
                    .Where(item =>
                        item.CustomerLocalId.HasValue)
                    .Select(item =>
                        item.CustomerLocalId!.Value)
                    .Distinct()
                    .ToList();

            var customerNames =
                customerIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : await _db.Customers
                        .AsNoTracking()
                        .Where(customer =>
                            customer.TenantId == tenantId &&
                            customerIds.Contains(
                                customer.Id))
                        .Select(customer =>
                            new
                            {
                                customer.Id,
                                customer.Name
                            })
                        .ToDictionaryAsync(
                            customer =>
                                customer.Id,
                            customer =>
                                customer.Name,
                            cancellationToken);

            var items =
                rawItems
                    .Select(item =>
                        new LocalSalesHistoryItemResult
                        {
                            LocalId =
                                item.LocalId,

                            ServerId =
                                item.ServerId,

                            LocalInvoiceNumber =
                                item.LocalInvoiceNumber,

                            ServerInvoiceNumber =
                                item.ServerInvoiceNumber,

                            InvoiceNumber =
                                string.IsNullOrWhiteSpace(
                                    item.ServerInvoiceNumber)
                                    ? item.LocalInvoiceNumber
                                    : item.ServerInvoiceNumber!,

                            SaleDateUtc =
                                item.SaleDateUtc,

                            CustomerLocalId =
                                item.CustomerLocalId,

                            CustomerServerId =
                                item.CustomerServerId,

                            CustomerName =
                                item.CustomerLocalId.HasValue &&
                                customerNames.TryGetValue(
                                    item.CustomerLocalId.Value,
                                    out var customerName)
                                    ? customerName
                                    : null,

                            TotalAmount =
                                item.TotalAmount,

                            PaidAmount =
                                item.PaidAmount,

                            ChangeAmount =
                                item.ChangeAmount,

                            Status =
                                item.Status,

                            PaymentStatus =
                                item.PaymentStatus,

                            SyncStatus =
                                item.SyncStatus
                        })
                    .ToList();

            return new LocalSalesHistoryPageResult
            {
                Items =
                    items,

                TotalCount =
                    totalCount,

                Page =
                    query.Page,

                PageSize =
                    query.PageSize
            };
        }

        public async Task<IReadOnlyList<LocalSalesHistoryCustomerResult>>
            SearchCustomersAsync(
                string? search,
                int maximumResults = 20,
                CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            maximumResults =
                Math.Clamp(
                    maximumResults,
                    1,
                    MaximumCustomerResults);

            var normalizedSearch =
                search?.Trim().ToLower();

            var customers =
                _db.Customers
                    .AsNoTracking()
                    .Where(customer =>
                        customer.TenantId == tenantId &&
                        !customer.IsDeleted &&
                        customer.IsActive);

            if (!string.IsNullOrWhiteSpace(
                    normalizedSearch))
            {
                customers =
                    customers.Where(customer =>
                        customer.Name
                            .ToLower()
                            .Contains(normalizedSearch) ||
                        (
                            customer.Phone != null &&
                            customer.Phone
                                .ToLower()
                                .Contains(normalizedSearch)
                        ) ||
                        (
                            customer.Email != null &&
                            customer.Email
                                .ToLower()
                                .Contains(normalizedSearch)
                        ));
            }

            return await customers
                .OrderBy(customer =>
                    customer.Name)
                .Take(maximumResults)
                .Select(customer =>
                    new LocalSalesHistoryCustomerResult
                    {
                        LocalId =
                            customer.Id,

                        ServerId =
                            customer.ServerId,

                        Name =
                            customer.Name,

                        Phone =
                            customer.Phone
                    })
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<LocalSalesHistoryDetailsResult?> GetByIdAsync(
    Guid localSaleId,
    CancellationToken cancellationToken = default)
        {
            if (localSaleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The local sale ID is required.",
                    nameof(localSaleId));
            }

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            var sale =
                await _db.Sales
                    .AsNoTracking()
                    .Include(item => item.Lines)
                    .Include(item => item.Payments)
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == localSaleId &&
                            item.TenantId == tenantId,
                        cancellationToken);

            if (sale == null)
            {
                return null;
            }

            string? customerName = null;

            if (sale.CustomerLocalId.HasValue)
            {
                customerName =
                    await _db.Customers
                        .AsNoTracking()
                        .Where(customer =>
                            customer.Id ==
                                sale.CustomerLocalId.Value &&
                            customer.TenantId == tenantId)
                        .Select(customer =>
                            customer.Name)
                        .FirstOrDefaultAsync(
                            cancellationToken);
            }

            var lines =
                sale.Lines
                    .Select(line =>
                    {
                        var grossAmount =
                            RoundMoney(
                                line.Quantity *
                                line.UnitPrice);

                        var percentageDiscount =
                            RoundMoney(
                                grossAmount *
                                line.DiscountPercent /
                                100m);

                        var totalDiscount =
                            RoundMoney(
                                Math.Clamp(
                                    percentageDiscount +
                                    line.DiscountAmount,
                                    0m,
                                    grossAmount));

                        var lineTotal =
                            RoundMoney(
                                grossAmount -
                                totalDiscount);

                        var divisor =
                            1m +
                            line.VatRate /
                            100m;

                        var amountExclVat =
                            divisor > 0m
                                ? RoundMoney(
                                    lineTotal /
                                    divisor)
                                : lineTotal;

                        var vatAmount =
                            RoundMoney(
                                lineTotal -
                                amountExclVat);

                        return new LocalSalesHistoryLineResult
                        {
                            LocalId =
                                line.Id,

                            ProductName =
                                line.ProductName,

                            ProductBarcode =
                                line.ProductBarcode,

                            Quantity =
                                line.Quantity,

                            UnitPrice =
                                line.UnitPrice,

                            DiscountPercent =
                                line.DiscountPercent,

                            DiscountAmount =
                                totalDiscount,

                            VatRate =
                                line.VatRate,

                            VatAmount =
                                vatAmount,

                            LineTotal =
                                lineTotal
                        };
                    })
                    .ToList();

            return new LocalSalesHistoryDetailsResult
            {
                LocalId =
                    sale.Id,

                InvoiceNumber =
                    string.IsNullOrWhiteSpace(
                        sale.ServerInvoiceNumber)
                        ? sale.LocalInvoiceNumber
                        : sale.ServerInvoiceNumber,

                SaleDateUtc =
                    sale.SaleDateUtc,

                CustomerName =
                    customerName,

                SubtotalAmount =
                    sale.SubtotalAmount,

                DiscountAmount =
                    sale.DiscountAmount,

                VatAmount =
                    sale.VatAmount,

                TotalAmount =
                    sale.TotalAmount,

                PaidAmount =
                    sale.PaidAmount,

                ChangeAmount =
                    sale.ChangeAmount,

                Status =
                    sale.Status,

                PaymentStatus =
                    sale.PaymentStatus,

                SyncStatus =
                    sale.SyncStatus,

                Lines =
                    lines,

                Payments =
                    sale.Payments
                        .OrderBy(payment =>
                            payment.PaidAtUtc)
                        .Select(payment =>
                            new LocalSalesHistoryPaymentResult
                            {
                                LocalId =
                                    payment.Id,

                                Method =
                                    payment.Method,

                                Amount =
                                    payment.Amount,

                                PaidAtUtc =
                                    payment.PaidAtUtc,

                                TransactionReference =
                                    payment.TransactionRef,

                                SyncStatus =
                                    payment.SyncStatus
                            })
                        .ToList()
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

        private static void ValidatePaging(
            int page,
            int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(page),
                    "Page must be greater than or equal to 1.");
            }

            if (pageSize < 1 ||
                pageSize > MaximumPageSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pageSize),
                    $"PageSize must be between 1 and " +
                    $"{MaximumPageSize}.");
            }
        }

        private static List<string> NormalizePaymentStatuses(
            IReadOnlyCollection<string>? source)
        {
            if (source == null ||
                source.Count == 0)
            {
                return new List<string>();
            }

            return source
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .Select(value =>
                    value.Trim().ToLowerInvariant() switch
                    {
                        "paid" =>
                            LocalPaymentStatus.Paid,

                        "partial" or
                        "partiallypaid" or
                        "partially paid" =>
                            LocalPaymentStatus.Partial,

                        "unpaid" or
                        "pending" =>
                            LocalPaymentStatus.Unpaid,

                        "refunded" =>
                            "Refunded",

                        "cancelled" or
                        "canceled" =>
                            "Cancelled",

                        _ =>
                            value.Trim()
                    })
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> NormalizeSaleStatuses(
            IReadOnlyCollection<string>? source)
        {
            if (source == null ||
                source.Count == 0)
            {
                return new List<string>();
            }

            return source
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .Select(value =>
                    value.Trim().ToLowerInvariant() switch
                    {
                        "completed" =>
                            LocalSaleStatus.Completed,

                        "pending" =>
                            "Pending",

                        "cancelled" or
                        "canceled" =>
                            "Cancelled",

                        _ =>
                            value.Trim()
                    })
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
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

        private sealed class RawSaleHistoryItem
        {
            public Guid LocalId { get; set; }

            public Guid? ServerId { get; set; }

            public string LocalInvoiceNumber { get; set; } =
                string.Empty;

            public string? ServerInvoiceNumber { get; set; }

            public DateTime SaleDateUtc { get; set; }

            public Guid? CustomerLocalId { get; set; }

            public Guid? CustomerServerId { get; set; }

            public decimal TotalAmount { get; set; }

            public decimal PaidAmount { get; set; }

            public decimal ChangeAmount { get; set; }

            public string Status { get; set; } =
                string.Empty;

            public string PaymentStatus { get; set; } =
                string.Empty;

            public string SyncStatus { get; set; } =
                string.Empty;
        }
    }
}
