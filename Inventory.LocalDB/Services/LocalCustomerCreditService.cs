using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Results;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.LocalDB.Services;

public sealed class LocalCustomerCreditService
    : ILocalCustomerCreditService
{
    private const string CustomerTransactionEntityName =
        "CustomerTransaction";

    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public LocalCustomerCreditService(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<List<LocalCustomerCreditResult>>
     GetCustomersWithBalanceAsync(
         CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();
        var customers =
            await _db.Customers
                .AsNoTracking()
                .Where(customer =>
                    customer.TenantId == tenantId &&
                    !customer.IsDeleted &&
                    customer.IsActive)
                .Select(customer =>
                    new LocalCustomerCreditResult
                    {
                        CustomerId =
                            customer.Id,

                        ServerCustomerId =
                            customer.ServerId,

                        Name =
                            customer.Name,

                        Balance =
                            customer.CurrentBalance,

                        SyncStatus =
                            customer.SyncStatus
                    })
                .ToListAsync(
                    cancellationToken);

        return customers
            .OrderByDescending(customer =>
                Math.Abs(customer.Balance))
            .ThenBy(customer =>
                customer.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<LocalCustomerDetailResult?>
        GetCustomerDetailAsync(
            Guid customerLocalId,
            CancellationToken cancellationToken = default)
    {
        if (customerLocalId == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer local id is required.",
                nameof(customerLocalId));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var customer =
            await _db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == customerLocalId &&
                        !item.IsDeleted,
                    cancellationToken);

        if (customer == null)
        {
            return null;
        }

        var transactions =
            await _db.CustomerTransactions
                .AsNoTracking()
                .Where(transaction =>
                    transaction.TenantId == tenantId &&
                    transaction.CustomerLocalId ==
                        customerLocalId)
                .OrderByDescending(transaction =>
                    transaction.TransactionDateUtc)
                .ThenByDescending(transaction =>
                    transaction.CreatedAtUtc)
                .ThenByDescending(transaction =>
                    transaction.Id)
                .Select(transaction =>
                    new LocalCustomerTransactionResult
                    {
                        Id =
                            transaction.Id,

                        ServerId =
                            transaction.ServerId,

                        Type =
                            transaction.Type,

                        Amount =
                            transaction.Amount,

                        BalanceBefore =
                            transaction.BalanceBefore,

                        BalanceAfter =
                            transaction.BalanceAfter,

                        Description =
                            transaction.Description,

                        TransactionDate =
                            transaction.TransactionDateUtc,

                        IsCash =
                            transaction.IsCash,

                        SyncStatus =
                            transaction.SyncStatus
                    })
                .ToListAsync(
                    cancellationToken);

        var sales =
            await _db.Sales
                .AsNoTracking()
                .Where(sale =>
                    sale.TenantId == tenantId &&
                    sale.CustomerLocalId ==
                        customerLocalId)
                .OrderByDescending(sale =>
                    sale.SaleDateUtc)
                .Select(sale =>
                    new LocalCustomerSaleSummaryResult
                    {
                        LocalSaleId =
                            sale.Id,

                        ServerSaleId =
                            sale.ServerId,

                        InvoiceNumber =
                            sale.ServerInvoiceNumber ??
                            sale.LocalInvoiceNumber,

                        SaleDate =
                            sale.SaleDateUtc,

                        PaymentStatus =
                            sale.PaymentStatus,

                        Status =
                            sale.Status,

                        TotalAmount =
                            sale.TotalAmount,

                        PaidAmount =
                            sale.PaidAmount,

                        SyncStatus =
                            sale.SyncStatus
                    })
                .ToListAsync(
                    cancellationToken);

        return new LocalCustomerDetailResult
        {
            CustomerId =
                customer.Id,

            Name =
                customer.Name,

            Email =
                customer.Email,

            Phone =
                customer.Phone,

            CurrentBalance =
                customer.CurrentBalance,

            Transactions =
                transactions,

            Sales =
                sales,

            TotalSales =
                RoundMoney(
                    sales.Sum(sale =>
                        sale.TotalAmount)),

            TotalPaid =
                RoundMoney(
                    sales.Sum(sale =>
                        sale.PaidAmount)),

            CreatedAt =
                customer.CreatedAtUtc
        };
    }

    public Task<LocalCustomerTransactionResult>
        RegisterPaymentAsync(
            RegisterCustomerPaymentRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return RegisterManualAsync(
            request.ClientOperationId,
            request.CustomerId,
            request.Amount,
            request.Description,
            request.IsCash,
            request.TransactionDateUtc,
            isPayment: true,
            cancellationToken);
    }

    public Task<LocalCustomerTransactionResult>
        RegisterRefundAsync(
            RegisterCustomerRefundRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return RegisterManualAsync(
            request.ClientOperationId,
            request.CustomerId,
            request.Amount,
            request.Description,
            request.IsCash,
            request.TransactionDateUtc,
            isPayment: false,
            cancellationToken);
    }

    public async Task RecordSaleCreditAsync(
        Guid customerLocalId,
        Guid saleLocalId,
        Guid? saleServerId,
        decimal amount,
        DateTime transactionDateUtc,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (saleLocalId == Guid.Empty)
        {
            throw new ArgumentException(
                "Sale local id is required.",
                nameof(saleLocalId));
        }

        await RecordAuthoritativeBalanceChangeAsync(
            customerLocalId,
            amount,
            LocalCustomerTransactionType.Credit,
            LocalCustomerTransactionOrigin.Sale,
            saleLocalId,
            saleServerId,
            transactionDateUtc,
            description ??
            "Customer credit created by local sale.",
            cancellationToken);
    }

    public async Task RecordReturnCreditAsync(
        Guid customerLocalId,
        Guid returnLocalId,
        decimal amount,
        DateTime transactionDateUtc,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (returnLocalId == Guid.Empty)
        {
            throw new ArgumentException(
                "Return local id is required.",
                nameof(returnLocalId));
        }

        /*
         * A credit return reduces customer debt. It can cross zero and
         * create a negative balance (store owes the customer).
         */
        await RecordAuthoritativeBalanceChangeAsync(
            customerLocalId,
            -amount,
            LocalCustomerTransactionType.Credit,
            LocalCustomerTransactionOrigin.Return,
            returnLocalId,
            null,
            transactionDateUtc,
            description ??
            "Customer credit created by local return.",
            cancellationToken);
    }

    private async Task<LocalCustomerTransactionResult>
        RegisterManualAsync(
            Guid clientOperationId,
            Guid customerLocalId,
            decimal amount,
            string? description,
            bool isCash,
            DateTime? transactionDateUtc,
            bool isPayment,
            CancellationToken cancellationToken)
    {
        if (customerLocalId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Customer is required.");
        }

        amount =
            RoundMoney(
                amount);

        if (amount <= 0m)
        {
            throw new InvalidOperationException(
                $"{(isPayment ? "Payment" : "Refund")} amount " +
                "must be greater than zero.");
        }

        if (clientOperationId == Guid.Empty)
        {
            clientOperationId =
                Guid.NewGuid();
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        await using var databaseTransaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var existing =
                await _db.CustomerTransactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        transaction =>
                            transaction.TenantId == tenantId &&
                            transaction.ClientOperationId ==
                                clientOperationId,
                        cancellationToken);

            if (existing != null)
            {
                await databaseTransaction.CommitAsync(
                    cancellationToken);

                return ToResult(
                    existing);
            }

            var customer =
                await _db.Customers
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id == customerLocalId &&
                            !item.IsDeleted &&
                            item.IsActive,
                        cancellationToken)
                ?? throw new KeyNotFoundException(
                    "The local customer was not found.");

            var balanceBefore =
                RoundMoney(
                    customer.CurrentBalance);

            decimal balanceAfter;

            if (isPayment)
            {
                if (balanceBefore <= 0m)
                {
                    throw new InvalidOperationException(
                        "Customer has no outstanding balance.");
                }

                if (amount > balanceBefore)
                {
                    throw new InvalidOperationException(
                        "Payment amount exceeds customer balance.");
                }

                balanceAfter =
                    RoundMoney(
                        balanceBefore -
                        amount);
            }
            else
            {
                if (balanceBefore >= 0m)
                {
                    throw new InvalidOperationException(
                        "Customer has no credit balance to refund.");
                }

                if (amount >
                    Math.Abs(balanceBefore))
                {
                    throw new InvalidOperationException(
                        "Refund amount exceeds customer credit balance.");
                }

                balanceAfter =
                    RoundMoney(
                        balanceBefore +
                        amount);
            }

            LocalCashSession? openSession =
                null;

            if (isCash)
            {
                openSession =
                    await _db.CashSessions
                        .FirstOrDefaultAsync(
                            session =>
                                session.TenantId == tenantId &&
                                session.Status ==
                                    LocalCashSessionStatus.Open,
                            cancellationToken)
                    ?? throw new InvalidOperationException(
                        "No open local cash session was found.");

                await ValidateAndCreateCashMovementAsync(
                    tenantId,
                    openSession,
                    clientOperationId,
                    customer,
                    amount,
                    isPayment,
                    transactionDateUtc,
                    cancellationToken);
            }

            var now =
                DateTime.UtcNow;

            var localTransaction =
                new LocalCustomerTransaction
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    ServerId =
                        null,

                    ClientOperationId =
                        clientOperationId,

                    CustomerLocalId =
                        customer.Id,

                    CustomerServerId =
                        customer.ServerId,

                    LocalCashSessionId =
                        openSession?.Id,

                    ServerCashSessionId =
                        openSession?.ServerId,

                    Type =
                        isPayment
                            ? LocalCustomerTransactionType.Payment
                            : LocalCustomerTransactionType.Refund,

                    Origin =
                        LocalCustomerTransactionOrigin.Manual,

                    UploadRequired =
                        true,

                    IsCash =
                        isCash,

                    Amount =
                        amount,

                    BalanceBefore =
                        balanceBefore,

                    BalanceAfter =
                        balanceAfter,

                    Description =
                        NormalizeNullable(
                            description) ??
                        (isPayment
                            ? "Customer payment"
                            : "Customer refund"),

                    TransactionDateUtc =
                        EnsureUtc(
                            transactionDateUtc ??
                            now),

                    SyncStatus =
                        SyncQueueStatus.Pending,

                    CreatedAtUtc =
                        now
                };

            customer.CurrentBalance =
                balanceAfter;

            customer.ModifiedAtUtc =
                now;

            _db.CustomerTransactions.Add(
                localTransaction);

            CreateQueueItem(
                localTransaction,
                tenantId);

            await _db.SaveChangesAsync(
                cancellationToken);

            await databaseTransaction.CommitAsync(
                cancellationToken);

            return ToResult(
                localTransaction);
        }
        catch
        {
            await databaseTransaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    private async Task RecordAuthoritativeBalanceChangeAsync(
        Guid customerLocalId,
        decimal signedAmount,
        string type,
        string origin,
        Guid sourceLocalId,
        Guid? sourceServerId,
        DateTime transactionDateUtc,
        string? description,
        CancellationToken cancellationToken)
    {
        if (customerLocalId == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer local id is required.",
                nameof(customerLocalId));
        }

        var amount =
            RoundMoney(
                Math.Abs(signedAmount));

        if (amount <= 0m)
        {
            return;
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var exists =
            await _db.CustomerTransactions
                .AsNoTracking()
                .AnyAsync(
                    transaction =>
                        transaction.TenantId == tenantId &&
                        transaction.CustomerLocalId ==
                            customerLocalId &&
                        transaction.Origin == origin &&
                        transaction.SaleLocalId ==
                            sourceLocalId,
                    cancellationToken);

        if (exists)
        {
            return;
        }

        var customer =
            await _db.Customers
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == customerLocalId &&
                        !item.IsDeleted,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "The local customer was not found.");

        var before =
            RoundMoney(
                customer.CurrentBalance);

        var after =
            RoundMoney(
                before +
                signedAmount);

        var now =
            DateTime.UtcNow;

        var localTransaction =
            new LocalCustomerTransaction
            {
                Id =
                    Guid.NewGuid(),

                TenantId =
                    tenantId,

                ServerId =
                    null,

                ClientOperationId =
                    Guid.NewGuid(),

                CustomerLocalId =
                    customer.Id,

                CustomerServerId =
                    customer.ServerId,

                SaleLocalId =
                    sourceLocalId,

                SaleServerId =
                    sourceServerId,

                Type =
                    type,

                Origin =
                    origin,

                UploadRequired =
                    false,

                IsCash =
                    false,

                Amount =
                    amount,

                BalanceBefore =
                    before,

                BalanceAfter =
                    after,

                Description =
                    NormalizeNullable(
                        description),

                TransactionDateUtc =
                    EnsureUtc(
                        transactionDateUtc),

                SyncStatus =
                    SyncQueueStatus.Pending,

                CreatedAtUtc =
                    now
            };

        customer.CurrentBalance =
            after;

        customer.ModifiedAtUtc =
            now;

        _db.CustomerTransactions.Add(
            localTransaction);

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    private async Task ValidateAndCreateCashMovementAsync(
        Guid tenantId,
        LocalCashSession session,
        Guid clientOperationId,
        LocalCustomer customer,
        decimal amount,
        bool isPayment,
        DateTime? transactionDateUtc,
        CancellationToken cancellationToken)
    {
        var movementAmounts =
            await _db.CashMovements
                .AsNoTracking()
                .Where(movement =>
                    movement.TenantId == tenantId &&
                    movement.LocalCashSessionId ==
                        session.Id)
                .Select(movement =>
                    movement.Amount)
                .ToListAsync(
                    cancellationToken);

        var movementTotal =
            movementAmounts.Sum();

        var drawerBefore =
            RoundMoney(
                session.OpeningAmount +
                movementTotal);

        var signedCashAmount =
            isPayment
                ? amount
                : -amount;

        var drawerAfter =
            RoundMoney(
                drawerBefore +
                signedCashAmount);

        if (drawerAfter < 0m)
        {
            throw new InvalidOperationException(
                $"Insufficient cash in local session. Available: " +
                $"€{drawerBefore:F2}; refund: €{amount:F2}.");
        }

        _db.CashMovements.Add(
            new LocalCashMovement
            {
                Id =
                    Guid.NewGuid(),

                TenantId =
                    tenantId,

                ServerId =
                    null,

                ClientOperationId =
                    Guid.NewGuid(),

                LocalCashSessionId =
                    session.Id,

                ServerCashSessionId =
                    session.ServerId,

                Type =
                    isPayment
                        ? LocalCustomerCashMovementType.Payment
                        : LocalCustomerCashMovementType.Refund,

                Amount =
                    signedCashAmount,

                ReferenceNumber =
                    $"CUSTOMER-{customer.Id:N}",

                LocalReferenceId =
                    clientOperationId,

                ServerReferenceId =
                    null,

                Notes =
                    isPayment
                        ? $"Cash customer payment for {customer.Name}"
                        : $"Cash customer refund for {customer.Name}",

                MovementDateUtc =
                    EnsureUtc(
                        transactionDateUtc ??
                        DateTime.UtcNow),

                SyncStatus =
                    SyncQueueStatus.Pending
            });
    }

    private void CreateQueueItem(
        LocalCustomerTransaction transaction,
        Guid tenantId)
    {
        var payload =
            JsonSerializer.Serialize(
                transaction,
                new JsonSerializerOptions
                {
                    WriteIndented =
                        false
                });

        _db.SyncQueueItems.Add(
            new SyncQueueItem
            {
                Id =
                    Guid.NewGuid(),

                TenantId =
                    tenantId,

                LocalEntityId =
                    transaction.Id,

                ClientOperationId =
                    transaction.ClientOperationId,

                EntityName =
                    CustomerTransactionEntityName,

                Operation =
                    SyncOperation.Create,

                PayloadJson =
                    payload,

                Status =
                    SyncQueueStatus.Pending,

                Attempts =
                    0,

                CreatedAtUtc =
                    DateTime.UtcNow
            });
    }

    private static LocalCustomerTransactionResult ToResult(
        LocalCustomerTransaction transaction)
    {
        return new LocalCustomerTransactionResult
        {
            Id =
                transaction.Id,

            ServerId =
                transaction.ServerId,

            Type =
                transaction.Type,

            Amount =
                transaction.Amount,

            BalanceBefore =
                transaction.BalanceBefore,

            BalanceAfter =
                transaction.BalanceAfter,

            Description =
                transaction.Description,

            TransactionDate =
                transaction.TransactionDateUtc,

            IsCash =
                transaction.IsCash,

            SyncStatus =
                transaction.SyncStatus
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

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
