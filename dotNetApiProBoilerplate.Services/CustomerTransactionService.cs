using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
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
        private readonly IRepository<CustomerTransaction> _customerTransactionRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<CashMovement> _cashMovementRepository;
        private readonly ICashSessionService _cashSessionService;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantContext _tenantContext;

        public CustomerTransactionService(
            IRepository<CustomerTransaction> customerTransactionRepository,
            IRepository<Customer> customerRepository,
            IRepository<CashMovement> cashMovementRepository,
            ICashSessionService cashSessionService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITenantContext tenantContext)
        {
            _customerTransactionRepository = customerTransactionRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantContext = tenantContext;
            _cashMovementRepository = cashMovementRepository;
            _cashSessionService = cashSessionService;
        }

        public async Task<CustomerTransactionResult> CreateAsync(CreateCustomerTransactionRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var transaction = _mapper.Map<CustomerTransaction>(request);

            transaction.Id = Guid.NewGuid();
            transaction.TenantId = tenantId;
            transaction.CreatedByUserId = userId;
            transaction.TransactionDate = DateTime.UtcNow;
            transaction.CreatedAt = DateTime.UtcNow;

            await _customerTransactionRepository.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CustomerTransactionResult>(transaction);
        }

        public async Task<CustomerTransactionResult> GetByIdAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var transaction = await _customerTransactionRepository.GetByIdAsync(id);

            if (transaction == null || transaction.IsDeleted || transaction.TenantId != tenantId)
            {
                throw new NotFoundException("CustomerTransaction", id);
            }

            return _mapper.Map<CustomerTransactionResult>(transaction);
        }

        public async Task<List<CustomerTransactionResult>> GetAllAsync()
        {
            var tenantId = _tenantContext.TenantId;
            var transactions = await _customerTransactionRepository.GetAllAsync();

            var activeTransactions = transactions
                .Where(c => !c.IsDeleted && c.TenantId == tenantId)
                .ToList();

            return _mapper.Map<List<CustomerTransactionResult>>(activeTransactions);
        }

        public async Task<CustomerTransactionResult> UpdateAsync(Guid id, UpdateCustomerTransactionRequest request)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var transaction = await _customerTransactionRepository.GetByIdAsync(id);
            if (transaction == null || transaction.IsDeleted || transaction.TenantId != tenantId)
            {
                throw new NotFoundException("CustomerTransaction", id);
            }

            _mapper.Map(request, transaction);
            transaction.ModifiedAt = DateTime.UtcNow;
            transaction.ModifiedByUserId = userId;

            _customerTransactionRepository.Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CustomerTransactionResult>(transaction);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;

            var transaction = await _customerTransactionRepository.GetByIdAsync(id);
            if (transaction == null || transaction.IsDeleted || transaction.TenantId != tenantId)
            {
                throw new NotFoundException("CustomerTransaction", id);
            }

            transaction.IsDeleted = true;
            transaction.DeletedAt = DateTime.UtcNow;
            transaction.DeletedByUserId = userId;
            transaction.ModifiedAt = DateTime.UtcNow;

            _customerTransactionRepository.Update(transaction);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<CustomerTransactionResult>> QueryAsync(CustomerTransactionQuery query)
        {
            var tenantId = _tenantContext.TenantId;

            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                });
            }

            var all = await _customerTransactionRepository.GetAllAsync();
            var filtered = all
                .Where(p => !p.IsDeleted && p.TenantId == tenantId)
                .AsQueryable();

            filtered = query.SortBy?.ToLower() switch
            {
                "transactiondate" => query.Desc
                    ? filtered.OrderByDescending(p => p.TransactionDate)
                    : filtered.OrderBy(p => p.TransactionDate),
                "type" => query.Desc
                    ? filtered.OrderByDescending(p => p.Type)
                    : filtered.OrderBy(p => p.Type),
                "amount" => query.Desc
                    ? filtered.OrderByDescending(p => p.Amount)
                    : filtered.OrderBy(p => p.Amount),
                _ => query.Desc
                    ? filtered.OrderByDescending(p => p.CreatedAt)
                    : filtered.OrderBy(p => p.CreatedAt)
            };

            var total = filtered.Count();
            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<CustomerTransactionResult>
            {
                Items = _mapper.Map<List<CustomerTransactionResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<CustomerTransactionResult> RegisterCustomerPaymentAsync(
            Guid customerId,
            decimal amount,
            string? description = null,
            bool isCash = true)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;
            var customer = await _customerRepository.GetByIdAsync(customerId);

            amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
                throw new NotFoundException("Customer", customerId);

            if (amount <= 0)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Amount", new[] { "Payment amount must be greater than 0." } }
                });


            // =========================
            // CUSTOMER BALANCE
            // =========================
            var lastTx = await _customerTransactionRepository.GetLastAsync(
                t => t.CustomerId == customerId
                     && !t.IsDeleted
                     && t.TenantId == tenantId,
                t => t.TransactionDate
            );

            var customerBalanceBefore = lastTx?.BalanceAfter ?? 0m;

            if (customerBalanceBefore <= 0)
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Balance", new[] { "Customer has no outstanding balance." } }
        });

            if (amount > customerBalanceBefore)
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Amount", new[] { "Payment amount exceeds customer balance." } }
        });

            var customerBalanceAfter = customerBalanceBefore - amount;

            // =========================
            // CUSTOMER TRANSACTION (AUTHORITATIVE)
            // =========================
            var paymentTx = new CustomerTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerId = customerId,
                Type = "Payment",
                Amount = amount,
                BalanceBefore = customerBalanceBefore,
                BalanceAfter = customerBalanceAfter,
                Description = description ?? "Customer payment",
                TransactionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _customerTransactionRepository.AddAsync(paymentTx);

            // =========================
            // CASH MOVEMENT (IF CASH)
            // =========================
            if (isCash)
            {
                var cashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

                var lastCash = await _cashMovementRepository.GetLastAsync(
                    m => m.CashSessionId == cashSessionId
                         && !m.IsDeleted
                         && m.TenantId == tenantId,
                    m => m.MovementDate
                );

                var cashBalanceBefore = lastCash?.BalanceAfter ?? 0m;
                var cashBalanceAfter = cashBalanceBefore + amount;

                await _cashMovementRepository.AddAsync(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CashSessionId = cashSessionId,
                    Type = CashMovementType.Deposit,
                    Amount = amount,
                    BalanceBefore = cashBalanceBefore,
                    BalanceAfter = cashBalanceAfter,
                    Reason = $"Customer debt payment (customerId={customerId})",
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                });
            }

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CustomerTransactionResult>(paymentTx);
        }

        public async Task<List<CustomerCreditResult>> GetCustomersWithBalanceAsync()
        {
            var tenantId = _tenantContext.TenantId;

            var customers = await _customerRepository.GetAllAsync();
            var transactions = await _customerTransactionRepository.GetAllAsync();

            var lastBalances = transactions
                .Where(t => !t.IsDeleted && t.TenantId == tenantId)
                .GroupBy(t => t.CustomerId)
                .Select(g => g.OrderByDescending(t => t.TransactionDate).First())
                .ToDictionary(t => t.CustomerId, t => t.BalanceAfter);

            return customers
                .Where(c => !c.IsDeleted && c.TenantId == tenantId)
                .Select(c => new CustomerCreditResult
                {
                    CustomerId = c.Id,
                    Name = c.Name,
                    Balance = lastBalances.TryGetValue(c.Id, out var b) ? b : 0m
                })
                .ToList();
        }

        public async Task<CustomerTransactionResult> RegisterCustomerRefundAsync(
    Guid customerId,
    decimal amount,
    string? description = null,
    bool isCash = true)
        {
            var tenantId = _tenantContext.TenantId;
            var userId = _tenantContext.UserId;
            var customer = await _customerRepository.GetByIdAsync(customerId);

            amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

            if (customer == null || customer.IsDeleted || customer.TenantId != tenantId)
                throw new NotFoundException("Customer", customerId);

            if (amount <= 0)
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Amount", new[] { "Refund amount must be greater than 0." } }
        });

            // Get customer balance
            var lastTx = await _customerTransactionRepository.GetLastAsync(
                t => t.CustomerId == customerId
                     && !t.IsDeleted
                     && t.TenantId == tenantId,
                t => t.TransactionDate
            );

            var customerBalanceBefore = lastTx?.BalanceAfter ?? 0m;

            // Customer balance must be negative (we owe them)
            if (customerBalanceBefore >= 0)
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Balance", new[] { "Customer has no credit balance to refund." } }
        });

            // Cannot refund more than we owe (balance is negative, so use absolute value)
            if (amount > Math.Abs(customerBalanceBefore))
                throw new ValidationException(new Dictionary<string, string[]>
        {
            { "Amount", new[] { "Refund amount exceeds credit balance." } }
        });

            // Refund increases the balance (makes it less negative, towards zero)
            var customerBalanceAfter = customerBalanceBefore + amount;

            // Create refund transaction
            var refundTx = new CustomerTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerId = customerId,
                Type = "Refund",
                Amount = amount,
                BalanceBefore = customerBalanceBefore,
                BalanceAfter = customerBalanceAfter,
                Description = description ?? "Customer refund",
                TransactionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _customerTransactionRepository.AddAsync(refundTx);

            // Create cash movement (withdrawal if cash)
            if (isCash)
            {
                var cashSessionId = await _cashSessionService.EnsureActiveSessionAsync();

                var lastCash = await _cashMovementRepository.GetLastAsync(
                    m => m.CashSessionId == cashSessionId
                         && !m.IsDeleted
                         && m.TenantId == tenantId,
                    m => m.MovementDate
                );

                var cashBalanceBefore = lastCash?.BalanceAfter ?? 0m;
                var cashBalanceAfter = cashBalanceBefore - amount;

                if (cashBalanceAfter < 0)
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        { "Cash", new[] { "Insufficient cash in session for refund." } }
                    });

                await _cashMovementRepository.AddAsync(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CashSessionId = cashSessionId,
                    Type = CashMovementType.Withdrawal,
                    Amount = amount,
                    BalanceBefore = cashBalanceBefore,
                    BalanceAfter = cashBalanceAfter,
                    Reason = $"Customer refund (customerId={customerId})",
                    MovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                });
            }

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CustomerTransactionResult>(refundTx);
        }


    }
}