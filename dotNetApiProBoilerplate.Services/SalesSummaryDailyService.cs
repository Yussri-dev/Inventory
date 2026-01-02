using AutoMapper;
using Inventory.Domain.Models;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.SalesSummaryDaily.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Exceptions;

namespace Inventory.Services
{
    public class SalesSummaryDailyService
    {
        private readonly IRepository<SalesSummaryDaily> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SalesSummaryDailyService(
            IRepository<SalesSummaryDaily> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // =========================
        // GET BY DATE
        // =========================
        public async Task<SalesSummaryDailyResult> GetByDateAsync(DateTime date)
        {
            var entity = (await _repository.GetAllAsync())
                .FirstOrDefault(x => x.Date.Date == date.Date && !x.IsDeleted);

            if (entity == null)
                throw new NotFoundException("SalesSummaryDaily", date);

            return _mapper.Map<SalesSummaryDailyResult>(entity);
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<SalesSummaryDailyResult>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();

            return _mapper.Map<List<SalesSummaryDailyResult>>(
                items.Where(x => !x.IsDeleted).ToList()
            );
        }

        // =========================
        // QUERY (REPORTING)
        // =========================
        public async Task<PagedResult<SalesSummaryDailyResult>> QueryAsync(SalesSummaryDailyQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Paging", new[] { "Invalid paging parameters." } }
                });
            }

            var summaries = (await _repository.GetAllAsync())
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (query.FromDate.HasValue)
                summaries = summaries.Where(x => x.Date >= query.FromDate.Value.Date);

            if (query.ToDate.HasValue)
                summaries = summaries.Where(x => x.Date <= query.ToDate.Value.Date);

            summaries = query.SortBy.ToLower() switch
            {
                "date" => query.Desc
                    ? summaries.OrderByDescending(x => x.Date)
                    : summaries.OrderBy(x => x.Date),

                "totalrevenue" => query.Desc
                    ? summaries.OrderByDescending(x => x.TotalRevenue)
                    : summaries.OrderBy(x => x.TotalRevenue),

                "totaltransactions" => query.Desc
                    ? summaries.OrderByDescending(x => x.TotalTransactions)
                    : summaries.OrderBy(x => x.TotalTransactions),

                _ => query.Desc
                    ? summaries.OrderByDescending(x => x.GeneratedAt)
                    : summaries.OrderBy(x => x.GeneratedAt)
            };

            var total = summaries.Count();

            var items = summaries
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<SalesSummaryDailyResult>
            {
                Items = _mapper.Map<List<SalesSummaryDailyResult>>(items),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
