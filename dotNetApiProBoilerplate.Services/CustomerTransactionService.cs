using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class CustomerTransactionService
    {
        private readonly IRepository<CustomerTransaction>
            _customerTransactionRepository;

        private readonly IRepository<Customer>
            _customerRepository;

        private readonly IRepository<Sale>
            _saleRepository;

        private readonly IRepository<CashMovement>
            _cashMovementRepository;

        private readonly IRepository<CashSession>
            _cashSessionRepository;

        private readonly IUnitOfWork
            _unitOfWork;

        private readonly IMapper
            _mapper;

        private readonly ITenantContext
            _tenantContext;

        public CustomerTransactionService(
            IRepository<CustomerTransaction>
                customerTransactionRepository,
            IRepository<Customer>
                customerRepository,
            IRepository<Sale>
                saleRepository,
            IRepository<CashMovement>
                cashMovementRepository,
            IRepository<CashSession>
                cashSessionRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            ArgumentNullException.ThrowIfNull(
                customerTransactionRepository);

            ArgumentNullException.ThrowIfNull(
                customerRepository);

            ArgumentNullException.ThrowIfNull(
                saleRepository);

            ArgumentNullException.ThrowIfNull(
                cashMovementRepository);

            ArgumentNullException.ThrowIfNull(
                cashSessionRepository);

            ArgumentNullException.ThrowIfNull(
                unitOfWork);

            ArgumentNullException.ThrowIfNull(
                mapper);

            ArgumentNullException.ThrowIfNull(
                tenantContext);

            _customerTransactionRepository =
                customerTransactionRepository;

            _customerRepository =
                customerRepository;

            _saleRepository =
                saleRepository;

            _cashMovementRepository =
                cashMovementRepository;

            _cashSessionRepository =
                cashSessionRepository;

            _unitOfWork =
                unitOfWork;

            _mapper =
                mapper;

            _tenantContext =
                tenantContext;
        }

        public async Task<CustomerTransactionResult>
            CreateAsync(
                CreateCustomerTransactionRequest request)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            var now =
                DateTime.UtcNow;

            var transaction =
                _mapper.Map<CustomerTransaction>(
                    request);

            transaction.Id =
                Guid.NewGuid();

            transaction.ClientOperationId =
                transaction.Id;

            transaction.TenantId =
                tenantId;

            transaction.CreatedByUserId =
                userId;

            transaction.TransactionDate =
                now;

            transaction.CreatedAt =
                now;

            await _customerTransactionRepository.AddAsync(
                transaction);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CustomerTransactionResult>(
                transaction);
        }

        public async Task<CustomerTransactionResult>
            GetByIdAsync(
                Guid id)
        {
            var tenantId =
                _tenantContext.TenantId;

            var transaction =
                await _customerTransactionRepository
                    .GetByIdAsync(id);

            if (transaction == null ||
                transaction.IsDeleted ||
                transaction.TenantId != tenantId)
            {
                throw new NotFoundException(
                    "CustomerTransaction",
                    id);
            }

            var result =
                _mapper.Map<CustomerTransactionResult>(
                    transaction);

            var customer =
                await _customerRepository.GetByIdAsync(
                    transaction.CustomerId);

            result.CustomerName =
                customer != null &&
                !customer.IsDeleted &&
                customer.TenantId == tenantId
                    ? customer.Name
                    : string.Empty;

            return result;
        }

        public async Task<List<CustomerTransactionResult>>
            GetAllAsync()
        {
            var tenantId =
                _tenantContext.TenantId;

            var transactions =
                await _customerTransactionRepository.GetAsync(
                    transaction =>
                        !transaction.IsDeleted &&
                        transaction.TenantId == tenantId);

            var customers =
                await _customerRepository.GetAsync(
                    customer =>
                        !customer.IsDeleted &&
                        customer.TenantId == tenantId);

            var customerNames =
                customers.ToDictionary(
                    customer => customer.Id,
                    customer => customer.Name);

            var results =
                _mapper.Map<List<CustomerTransactionResult>>(
                    transactions);

            foreach (var result in results)
            {
                result.CustomerName =
                    customerNames.TryGetValue(
                        result.CustomerId,
                        out var customerName)
                        ? customerName
                        : string.Empty;
            }

            return results;
        }

        public async Task<CustomerTransactionResult>
            UpdateAsync(
                Guid id,
                UpdateCustomerTransactionRequest request)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            var transaction =
                await _customerTransactionRepository
                    .GetByIdAsync(id);

            if (transaction == null ||
                transaction.IsDeleted ||
                transaction.TenantId != tenantId)
            {
                throw new NotFoundException(
                    "CustomerTransaction",
                    id);
            }

            _mapper.Map(
                request,
                transaction);

            transaction.ModifiedAt =
                DateTime.UtcNow;

            transaction.ModifiedByUserId =
                userId;

            _customerTransactionRepository.Update(
                transaction);

            await _unitOfWork.SaveChangesAsync();

            var result =
                _mapper.Map<CustomerTransactionResult>(
                    transaction);

            var customer =
                await _customerRepository.GetByIdAsync(
                    transaction.CustomerId);

            result.CustomerName =
                customer != null &&
                !customer.IsDeleted &&
                customer.TenantId == tenantId
                    ? customer.Name
                    : string.Empty;

            return result;
        }

        public async Task<bool> DeleteAsync(
            Guid id)
        {
            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            var transaction =
                await _customerTransactionRepository
                    .GetByIdAsync(id);

            if (transaction == null ||
                transaction.IsDeleted ||
                transaction.TenantId != tenantId)
            {
                throw new NotFoundException(
                    "CustomerTransaction",
                    id);
            }

            var now =
                DateTime.UtcNow;

            transaction.IsDeleted =
                true;

            transaction.DeletedAt =
                now;

            transaction.DeletedByUserId =
                userId;

            transaction.ModifiedAt =
                now;

            transaction.ModifiedByUserId =
                userId;

            _customerTransactionRepository.Update(
                transaction);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<PagedResult<CustomerTransactionResult>>
            QueryAsync(
                CustomerTransactionQuery query)
        {
            ArgumentNullException.ThrowIfNull(
                query);

            var tenantId =
                _tenantContext.TenantId;

            if (query.Page < 1 ||
                query.PageSize < 1 ||
                query.PageSize > 100)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(query.PageSize),
                            new[]
                            {
                                "PageSize must be between 1 and 100."
                            }
                        }
                    });
            }

            var transactions =
                await _customerTransactionRepository.GetAsync(
                    transaction =>
                        !transaction.IsDeleted &&
                        transaction.TenantId == tenantId);

            var filtered =
                transactions.AsQueryable();

            filtered =
                query.SortBy?.ToLowerInvariant() switch
                {
                    "transactiondate" =>
                        query.Desc
                            ? filtered.OrderByDescending(
                                transaction =>
                                    transaction.TransactionDate)
                            : filtered.OrderBy(
                                transaction =>
                                    transaction.TransactionDate),

                    "type" =>
                        query.Desc
                            ? filtered.OrderByDescending(
                                transaction =>
                                    transaction.Type)
                            : filtered.OrderBy(
                                transaction =>
                                    transaction.Type),

                    "amount" =>
                        query.Desc
                            ? filtered.OrderByDescending(
                                transaction =>
                                    transaction.Amount)
                            : filtered.OrderBy(
                                transaction =>
                                    transaction.Amount),

                    _ =>
                        query.Desc
                            ? filtered.OrderByDescending(
                                transaction =>
                                    transaction.CreatedAt)
                            : filtered.OrderBy(
                                transaction =>
                                    transaction.CreatedAt)
                };

            var total =
                filtered.Count();

            var items =
                filtered
                    .Skip(
                        (query.Page - 1) *
                        query.PageSize)
                    .Take(
                        query.PageSize)
                    .ToList();

            var customers =
                await _customerRepository.GetAsync(
                    customer =>
                        !customer.IsDeleted &&
                        customer.TenantId == tenantId);

            var customerNames =
                customers.ToDictionary(
                    customer => customer.Id,
                    customer => customer.Name);

            var results =
                _mapper.Map<List<CustomerTransactionResult>>(
                    items);

            foreach (var result in results)
            {
                result.CustomerName =
                    customerNames.TryGetValue(
                        result.CustomerId,
                        out var customerName)
                        ? customerName
                        : string.Empty;
            }

            return new PagedResult<CustomerTransactionResult>
            {
                Items =
                    results,

                TotalCount =
                    total,

                Page =
                    query.Page,

                PageSize =
                    query.PageSize
            };
        }

        public async Task<CustomerTransactionResult>
            RegisterCustomerPaymentAsync(
                RegisterCustomerPaymentRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            cancellationToken.ThrowIfCancellationRequested();

            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            ValidateRequiredIdentifiers(
                request.ClientOperationId,
                request.CustomerId);

            var amount =
                Math.Round(
                    request.Amount,
                    2,
                    MidpointRounding.AwayFromZero);

            if (amount <= 0m)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(request.Amount),
                            new[]
                            {
                                "Payment amount must be greater than 0."
                            }
                        }
                    });
            }

            var description =
                string.IsNullOrWhiteSpace(
                    request.Description)
                    ? "Customer payment"
                    : request.Description.Trim();

            var cashSessionId =
                ValidateAndResolveCashSessionId(
                    request.IsCash,
                    request.CashSessionId,
                    "payment");

            var existingTransactions =
                await _customerTransactionRepository.GetAsync(
                    transaction =>
                        !transaction.IsDeleted &&
                        transaction.TenantId == tenantId &&
                        transaction.ClientOperationId ==
                            request.ClientOperationId);

            var existingTransaction =
                existingTransactions.SingleOrDefault();

            if (existingTransaction != null)
            {
                var sameOperation =
                    string.Equals(
                        existingTransaction.Type,
                        "Payment",
                        StringComparison.Ordinal) &&
                    existingTransaction.CustomerId ==
                        request.CustomerId &&
                    existingTransaction.Amount ==
                        amount &&
                    existingTransaction.IsCash ==
                        request.IsCash &&
                    existingTransaction.CashSessionId ==
                        cashSessionId &&
                    string.Equals(
                        existingTransaction.Description,
                        description,
                        StringComparison.Ordinal);

                if (!sameOperation)
                {
                    throw new ConflictException(
                        $"Client operation " +
                        $"'{request.ClientOperationId}' " +
                        "is already linked to a different " +
                        "customer transaction.");
                }

                return await MapResultWithCustomerNameAsync(
                    existingTransaction,
                    tenantId);
            }

            var customer =
                await _customerRepository.GetByIdAsync(
                    request.CustomerId);

            if (customer == null ||
                customer.IsDeleted ||
                customer.TenantId != tenantId)
            {
                throw new NotFoundException(
                    "Customer",
                    request.CustomerId);
            }

            CashSession? selectedCashSession =
                null;

            if (cashSessionId.HasValue)
            {
                selectedCashSession =
                    await _cashSessionRepository.GetByIdAsync(
                        cashSessionId.Value);

                if (selectedCashSession == null ||
                    selectedCashSession.IsDeleted ||
                    selectedCashSession.TenantId != tenantId)
                {
                    throw new NotFoundException(
                        "CashSession",
                        cashSessionId.Value);
                }
            }

            var customerBalanceBefore =
                Math.Round(
                    customer.CurrentBalance,
                    2,
                    MidpointRounding.AwayFromZero);

            if (customerBalanceBefore <= 0m)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            "Balance",
                            new[]
                            {
                                "Customer has no outstanding balance."
                            }
                        }
                    });
            }

            if (amount > customerBalanceBefore)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(request.Amount),
                            new[]
                            {
                                "Payment amount exceeds customer balance."
                            }
                        }
                    });
            }

            var customerBalanceAfter =
                customerBalanceBefore -
                amount;

            var now =
                DateTime.UtcNow;

            var transactionDateUtc =
                ResolveUtcDate(
                    request.TransactionDateUtc,
                    now);

            var paymentTransaction =
                new CustomerTransaction
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    ClientOperationId =
                        request.ClientOperationId,

                    CustomerId =
                        request.CustomerId,

                    Type =
                        "Payment",

                    Amount =
                        amount,

                    BalanceBefore =
                        customerBalanceBefore,

                    BalanceAfter =
                        customerBalanceAfter,

                    Description =
                        description,

                    IsCash =
                        request.IsCash,

                    CashSessionId =
                        cashSessionId,

                    TransactionDate =
                        transactionDateUtc,

                    CreatedAt =
                        now,

                    CreatedByUserId =
                        userId
                };

            await _customerTransactionRepository.AddAsync(
                paymentTransaction);

            customer.CurrentBalance =
                customerBalanceAfter;

            customer.ModifiedAt =
                now;

            customer.ModifiedByUserId =
                userId;

            _customerRepository.Update(
                customer);

            if (cashSessionId.HasValue)
            {
                var lastCashMovement =
                    await _cashMovementRepository.GetLastAsync(
                        movement =>
                            movement.TenantId == tenantId &&
                            !movement.IsDeleted &&
                            movement.CashSessionId ==
                                cashSessionId.Value,
                        movement =>
                            movement.CreatedAt);

                var cashBalanceBefore =
                    Math.Round(
                        lastCashMovement?.BalanceAfter ?? 0m,
                        2,
                        MidpointRounding.AwayFromZero);

                var cashBalanceAfter =
                    Math.Round(
                        cashBalanceBefore + amount,
                        2,
                        MidpointRounding.AwayFromZero);

                ReconcileClosedCashSession(
                    selectedCashSession!,
                    cashBalanceAfter,
                    now,
                    userId);

                await _cashMovementRepository.AddAsync(
                    new CashMovement
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        CashSessionId =
                            cashSessionId.Value,

                        Type =
                            CashMovementType.Deposit,

                        Amount =
                            amount,

                        BalanceBefore =
                            cashBalanceBefore,

                        BalanceAfter =
                            cashBalanceAfter,

                        Reason =
                            $"Customer debt payment " +
                            $"(customerId={request.CustomerId}, " +
                            $"clientOperationId=" +
                            $"{request.ClientOperationId})",

                        MovementDate =
                            transactionDateUtc,

                        CreatedAt =
                            now,

                        CreatedByUserId =
                            userId
                    });
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _unitOfWork.SaveChangesAsync();

            var paymentResult =
                _mapper.Map<CustomerTransactionResult>(
                    paymentTransaction);

            paymentResult.CustomerName =
                customer.Name;

            return paymentResult;
        }

        public async Task<List<CustomerCreditResult>>
            GetCustomersWithBalanceAsync()
        {
            var tenantId =
                _tenantContext.TenantId;

            var customers =
                await _customerRepository.GetAsync(
                    customer =>
                        !customer.IsDeleted &&
                        customer.TenantId == tenantId);

            return customers
                .Select(
                    customer =>
                        new CustomerCreditResult
                        {
                            CustomerId =
                                customer.Id,

                            Name =
                                customer.Name,

                            Balance =
                                customer.CurrentBalance
                        })
                .ToList();
        }

        public async Task<CustomerTransactionResult>
            RegisterCustomerRefundAsync(
                RegisterCustomerRefundRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            cancellationToken.ThrowIfCancellationRequested();

            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            ValidateRequiredIdentifiers(
                request.ClientOperationId,
                request.CustomerId);

            var amount =
                Math.Round(
                    request.Amount,
                    2,
                    MidpointRounding.AwayFromZero);

            if (amount <= 0m)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(request.Amount),
                            new[]
                            {
                                "Refund amount must be greater than 0."
                            }
                        }
                    });
            }

            var description =
                string.IsNullOrWhiteSpace(
                    request.Description)
                    ? "Customer refund"
                    : request.Description.Trim();

            var cashSessionId =
                ValidateAndResolveCashSessionId(
                    request.IsCash,
                    request.CashSessionId,
                    "refund");

            var existingTransactions =
                await _customerTransactionRepository.GetAsync(
                    transaction =>
                        !transaction.IsDeleted &&
                        transaction.TenantId == tenantId &&
                        transaction.ClientOperationId ==
                            request.ClientOperationId);

            var existingTransaction =
                existingTransactions.SingleOrDefault();

            if (existingTransaction != null)
            {
                var sameOperation =
                    string.Equals(
                        existingTransaction.Type,
                        "Refund",
                        StringComparison.Ordinal) &&
                    existingTransaction.CustomerId ==
                        request.CustomerId &&
                    existingTransaction.Amount ==
                        amount &&
                    existingTransaction.IsCash ==
                        request.IsCash &&
                    existingTransaction.CashSessionId ==
                        cashSessionId &&
                    string.Equals(
                        existingTransaction.Description,
                        description,
                        StringComparison.Ordinal);

                if (!sameOperation)
                {
                    throw new ConflictException(
                        $"Client operation " +
                        $"'{request.ClientOperationId}' " +
                        "is already linked to a different " +
                        "customer transaction.");
                }

                return await MapResultWithCustomerNameAsync(
                    existingTransaction,
                    tenantId);
            }

            var customer =
                await _customerRepository.GetByIdAsync(
                    request.CustomerId);

            if (customer == null ||
                customer.IsDeleted ||
                customer.TenantId != tenantId)
            {
                throw new NotFoundException(
                    "Customer",
                    request.CustomerId);
            }

            CashSession? selectedCashSession =
                null;

            if (cashSessionId.HasValue)
            {
                selectedCashSession =
                    await _cashSessionRepository.GetByIdAsync(
                        cashSessionId.Value);

                if (selectedCashSession == null ||
                    selectedCashSession.IsDeleted ||
                    selectedCashSession.TenantId != tenantId)
                {
                    throw new NotFoundException(
                        "CashSession",
                        cashSessionId.Value);
                }
            }

            var customerBalanceBefore =
                Math.Round(
                    customer.CurrentBalance,
                    2,
                    MidpointRounding.AwayFromZero);

            if (customerBalanceBefore >= 0m)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            "Balance",
                            new[]
                            {
                                "Customer has no credit balance to refund."
                            }
                        }
                    });
            }

            if (amount >
                Math.Abs(customerBalanceBefore))
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(request.Amount),
                            new[]
                            {
                                "Refund amount exceeds customer " +
                                "credit balance."
                            }
                        }
                    });
            }

            var customerBalanceAfter =
                customerBalanceBefore +
                amount;

            var now =
                DateTime.UtcNow;

            var transactionDateUtc =
                ResolveUtcDate(
                    request.TransactionDateUtc,
                    now);

            CashMovement? cashMovement =
                null;

            if (cashSessionId.HasValue)
            {
                var lastCashMovement =
                    await _cashMovementRepository.GetLastAsync(
                        movement =>
                            movement.TenantId == tenantId &&
                            !movement.IsDeleted &&
                            movement.CashSessionId ==
                                cashSessionId.Value,
                        movement =>
                            movement.CreatedAt);

                var cashBalanceBefore =
                    Math.Round(
                        lastCashMovement?.BalanceAfter ?? 0m,
                        2,
                        MidpointRounding.AwayFromZero);

                var cashBalanceAfter =
                    Math.Round(
                        cashBalanceBefore - amount,
                        2,
                        MidpointRounding.AwayFromZero);

                if (cashBalanceAfter < 0m)
                {
                    throw new ValidationException(
                        new Dictionary<string, string[]>
                        {
                            {
                                "Cash",
                                new[]
                                {
                                    "Insufficient cash in the selected " +
                                    "session for this refund."
                                }
                            }
                        });
                }

                ReconcileClosedCashSession(
                    selectedCashSession!,
                    cashBalanceAfter,
                    now,
                    userId);

                cashMovement =
                    new CashMovement
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        CashSessionId =
                            cashSessionId.Value,

                        Type =
                            CashMovementType.Withdrawal,

                        Amount =
                            amount,

                        BalanceBefore =
                            cashBalanceBefore,

                        BalanceAfter =
                            cashBalanceAfter,

                        Reason =
                            $"Customer refund " +
                            $"(customerId={request.CustomerId}, " +
                            $"clientOperationId=" +
                            $"{request.ClientOperationId})",

                        MovementDate =
                            transactionDateUtc,

                        CreatedAt =
                            now,

                        CreatedByUserId =
                            userId
                    };
            }

            var refundTransaction =
                new CustomerTransaction
                {
                    Id =
                        Guid.NewGuid(),

                    TenantId =
                        tenantId,

                    ClientOperationId =
                        request.ClientOperationId,

                    CustomerId =
                        request.CustomerId,

                    Type =
                        "Refund",

                    Amount =
                        amount,

                    BalanceBefore =
                        customerBalanceBefore,

                    BalanceAfter =
                        customerBalanceAfter,

                    Description =
                        description,

                    IsCash =
                        request.IsCash,

                    CashSessionId =
                        cashSessionId,

                    TransactionDate =
                        transactionDateUtc,

                    CreatedAt =
                        now,

                    CreatedByUserId =
                        userId
                };

            await _customerTransactionRepository.AddAsync(
                refundTransaction);

            if (cashMovement != null)
            {
                await _cashMovementRepository.AddAsync(
                    cashMovement);
            }

            customer.CurrentBalance =
                customerBalanceAfter;

            customer.ModifiedAt =
                now;

            customer.ModifiedByUserId =
                userId;

            _customerRepository.Update(
                customer);

            cancellationToken.ThrowIfCancellationRequested();

            await _unitOfWork.SaveChangesAsync();

            var refundResult =
                _mapper.Map<CustomerTransactionResult>(
                    refundTransaction);

            refundResult.CustomerName =
                customer.Name;

            return refundResult;
        }

        private async Task<CustomerTransactionResult>
            MapResultWithCustomerNameAsync(
                CustomerTransaction transaction,
                Guid tenantId)
        {
            var result =
                _mapper.Map<CustomerTransactionResult>(
                    transaction);

            var customer =
                await _customerRepository.GetByIdAsync(
                    transaction.CustomerId);

            result.CustomerName =
                customer != null &&
                !customer.IsDeleted &&
                customer.TenantId == tenantId
                    ? customer.Name
                    : string.Empty;

            return result;
        }

        private static void ValidateRequiredIdentifiers(
            Guid clientOperationId,
            Guid customerId)
        {
            if (clientOperationId == Guid.Empty)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(clientOperationId),
                            new[]
                            {
                                "ClientOperationId must not be empty."
                            }
                        }
                    });
            }

            if (customerId == Guid.Empty)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(customerId),
                            new[]
                            {
                                "CustomerId must not be empty."
                            }
                        }
                    });
            }
        }

        private static Guid? ValidateAndResolveCashSessionId(
            bool isCash,
            Guid? cashSessionId,
            string operationName)
        {
            if (!isCash)
            {
                return null;
            }

            if (!cashSessionId.HasValue ||
                cashSessionId.Value == Guid.Empty)
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        {
                            nameof(cashSessionId),
                            new[]
                            {
                                $"CashSessionId is required for a cash " +
                                $"{operationName}."
                            }
                        }
                    });
            }

            return cashSessionId.Value;
        }

        private static DateTime ResolveUtcDate(
            DateTime? requestedDateUtc,
            DateTime fallbackUtc)
        {
            if (!requestedDateUtc.HasValue)
            {
                return fallbackUtc;
            }

            var value =
                requestedDateUtc.Value;

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

        private void ReconcileClosedCashSession(
            CashSession cashSession,
            decimal expectedCash,
            DateTime modifiedAtUtc,
            Guid userId)
        {
            ArgumentNullException.ThrowIfNull(
                cashSession);

            if (!cashSession.ClosedAt.HasValue)
            {
                return;
            }

            cashSession.ClosingAmountExpected =
                Math.Round(
                    expectedCash,
                    2,
                    MidpointRounding.AwayFromZero);

            cashSession.Difference =
                Math.Round(
                    cashSession.ClosingAmountCounted -
                    cashSession.ClosingAmountExpected,
                    2,
                    MidpointRounding.AwayFromZero);

            cashSession.ModifiedAt =
                modifiedAtUtc;

            cashSession.ModifiedByUserId =
                userId;

            _cashSessionRepository.Update(
                cashSession);
        }

        public async Task<CustomerDetailResult>
            GetCustomerDetailAsync(
                Guid customerId)
        {
            var tenantId =
                _tenantContext.TenantId;

            var customer =
                await _customerRepository.GetByIdAsync(
                    customerId);

            if (customer == null ||
                customer.IsDeleted ||
                customer.TenantId != tenantId)
            {
                throw new NotFoundException(
                    "Customer",
                    customerId);
            }

            var transactions =
                (await _customerTransactionRepository.GetAsync(
                    transaction =>
                        transaction.CustomerId == customerId &&
                        !transaction.IsDeleted &&
                        transaction.TenantId == tenantId))
                .OrderByDescending(
                    transaction =>
                        transaction.TransactionDate)
                .ThenByDescending(
                    transaction =>
                        transaction.CreatedAt)
                .ThenByDescending(
                    transaction =>
                        transaction.Id)
                .ToList();

            var sales =
                (await _saleRepository.GetAsync(
                    sale =>
                        sale.CustomerId == customerId &&
                        !sale.IsDeleted &&
                        sale.TenantId == tenantId))
                .OrderByDescending(
                    sale =>
                        sale.SaleDate)
                .ToList();

            var transactionResults =
                _mapper.Map<List<CustomerTransactionResult>>(
                    transactions);

            foreach (var result in transactionResults)
            {
                result.CustomerName =
                    customer.Name;
            }

            return new CustomerDetailResult
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
                    transactionResults,

                Sales =
                    _mapper.Map<List<SaleSummaryResult>>(
                        sales),

                TotalSales =
                    sales.Sum(
                        sale =>
                            sale.TotalAmount),

                TotalPaid =
                    sales.Sum(
                        sale =>
                            sale.PaidAmount),

                CreatedAt =
                    customer.CreatedAt
            };
        }
    }
}