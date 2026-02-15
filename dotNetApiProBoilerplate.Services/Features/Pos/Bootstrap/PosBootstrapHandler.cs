using AutoMapper;
using Inventory.Domain.Entities;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.GlobalRequests.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Stock.Results;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Context;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Pos.Bootstrap
{
    public sealed class PosBootstrapHandler
        : IRequestHandler<PosBootstrapQuery, PosBootstrapResult>
    {
        private readonly IRepository<Product> _products;
        private readonly IRepository<Stock> _stocks;
        private readonly IRepository<Customer> _customers;
        private readonly ITenantContext _tenant;
        private readonly IMapper _mapper;

        public PosBootstrapHandler(
            IRepository<Product> products,
            IRepository<Stock> stocks,
            IRepository<Customer> customers,
            ITenantContext tenant,
            IMapper mapper)
        {
            _products = products;
            _stocks = stocks;
            _customers = customers;
            _tenant = tenant;
            _mapper = mapper;
        }


        public async Task<PosBootstrapResult> Handle(
            PosBootstrapQuery request,
            CancellationToken ct)
        {
            var tenantId = _tenant.TenantId;

            var products = await _products.GetAsync(
                p => !p.IsDeleted && p.TenantId == tenantId);

            var stocks = await _stocks.GetAsync(
                s => !s.IsDeleted && s.TenantId == tenantId);

            var customers = await _customers.GetAsync(
                c => !c.IsDeleted && c.TenantId == tenantId);

            return new PosBootstrapResult
            {
                ServerTime = DateTime.UtcNow,
                Products = _mapper.Map<List<ProductResult>>(products),
                Stocks = _mapper.Map<List<StockResult>>(stocks),
                Customers = _mapper.Map<List<CustomerResult>>(customers),
                Config = new PosConfigResult
                {
                    Currency = "EUR",
                    DefaultVatRate = 21,
                    AllowNegativeStock = false
                }
            };

        }
    }
}
