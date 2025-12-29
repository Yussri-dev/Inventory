using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.PurchasePayments.Requests;
using Inventory.Dto.PurchasePayments.Results;
using Inventory.Dto.Queries;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class PurchasePaymentService
    {
        private readonly IRepository<PurchasePayment> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PurchasePaymentService(
            IRepository<PurchasePayment> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CREATE
        // CREATE
        public async Task<PurchasePaymentResult> CreateAsync(CreatePurchasePaymentRequest request)
        {
            if (request.Amount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "QuantityOrdered", new[] { "QuantityOrdered must be > 0." } }
                });

            }

            var purchasePayment = _mapper.Map<PurchasePayment>(request);

            purchasePayment.Id = Guid.NewGuid();
            purchasePayment.Amount = request.Amount;

            await _repository.AddAsync(purchasePayment);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchasePaymentResult>(purchasePayment);
        }

        // GET BY ID
        public async Task<PurchasePaymentResult> GetByIdAsync(Guid id)
        {
            var purchasePayment = await _repository.GetByIdAsync(id);

            //if (purchasePayment is null || purchasePayment.IsDeleted)
            //{
            //    throw new NotFoundException("PurchasePayment", id);
            //}

            return _mapper.Map<PurchasePaymentResult>(purchasePayment);
        }

        // GET ALL
        public async Task<List<PurchasePaymentResult>> GetAllAsync()
        {
            var purchasePayments = await _repository.GetAllAsync();

            //// Filter out soft-deleted purchasePayments
            //var activePurchasePayments = purchasePayments.Where(p => !p.IsDeleted).ToList();

            return _mapper.Map<List<PurchasePaymentResult>>(purchasePayments);
        }

        // UPDATE
        public async Task<PurchasePaymentResult> UpdateAsync(Guid id, UpdatePurchasePaymentRequest request)
        {
            var purchasePayment = await _repository.GetByIdAsync(id);

            if (request.Amount <= 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Amount", new[] { "Amount must be > 0." } }
                });

            }

            //if (request.PaymentAmountInclVat <= 0)
            //{
            //    throw new ValidationException(new Dictionary<string, string[]>
            //    {
            //        { "PaymentAmountInclVat", new[] { "PaymentAmountInclVat must be > 0." } }
            //    });
            //}

            _mapper.Map(request, purchasePayment);

            _repository.Update(purchasePayment);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PurchasePaymentResult>(purchasePayment);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            var purchasePayment = await _repository.GetByIdAsync(id);

            _repository.Update(purchasePayment);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // PAGINATION + FILTERING + SORTING
        public async Task<PagedResult<PurchasePaymentResult>> QueryAsync(PurchasePaymentQuery query)
        {
            // Validate query parameters
            if (query.Page < 1)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Page", new[] { "Page must be greater than or equal to 1." } }
                };
                throw new ValidationException(errors);
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "PageSize", new[] { "PageSize must be between 1 and 100." } }
                };
                throw new ValidationException(errors);
            }

            var filtered = await _repository.GetAllAsync();

            var total = filtered.Count();

            var items = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<PurchasePaymentResult>
            {
                Items = _mapper.Map<List<PurchasePaymentResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}