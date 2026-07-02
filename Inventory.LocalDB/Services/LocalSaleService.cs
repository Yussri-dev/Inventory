
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.LocalDB.Services
{
    public class LocalSaleService : ILocalSaleService
    {
        private readonly PosLocalDbContext _db;

        public LocalSaleService(PosLocalDbContext db)
        {
            _db = db;
        }

        public async Task<LocalSale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Sales
                .AsNoTracking()
                .Include(x => x.Lines)
                .Include(x => x.Payments)
                .Include(x => x.LocalCashSession)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<List<LocalSale>> GetTodaySalesAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            return await _db.Sales
                .AsNoTracking()
                .Include(x => x.Lines)
                .Include(x => x.Payments)
                .Where(x => x.SaleDateUtc >= today && x.SaleDateUtc < tomorrow)
                .OrderByDescending(x => x.SaleDateUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<LocalSale> CreateAsync(LocalSale sale, CancellationToken cancellationToken = default)
        {
            if (sale == null)
                throw new ArgumentNullException(nameof(sale));

            if (sale.Lines == null || sale.Lines.Count == 0)
                throw new InvalidOperationException("Sale must contain at least one line.");

            if (sale.Payments == null || sale.Payments.Count == 0)
                throw new InvalidOperationException("Sale must contain at least one payment.");

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var openSession = await _db.CashSessions
                .FirstOrDefaultAsync(x => x.Status == LocalCashSessionStatus.Open, cancellationToken);

            if (openSession == null)
                throw new InvalidOperationException("No open cash session found.");

            PrepareSaleHeader(sale, openSession.Id, openSession.ServerId);

            RecalculateSaleTotals(sale);

            ValidatePaymentAmount(sale);

            _db.Sales.Add(sale);

            await ApplyStockMovementsAsync(sale, cancellationToken);

            await CreateCashMovementIfNeededAsync(sale, openSession.Id, cancellationToken);

            CreateSyncQueueItem(sale);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return sale;
        }

        private static void PrepareSaleHeader(LocalSale sale, Guid localCashSessionId, Guid? serverCashSessionId)
        {
            var now = DateTime.UtcNow;

            if (sale.Id == Guid.Empty)
                sale.Id = Guid.NewGuid();

            if (sale.ClientOperationId == Guid.Empty)
                sale.ClientOperationId = Guid.NewGuid();

            if (string.IsNullOrWhiteSpace(sale.LocalInvoiceNumber))
                sale.LocalInvoiceNumber = GenerateLocalInvoiceNumber(now);

            sale.LocalCashSessionId = localCashSessionId;
            sale.CashSessionServerId = serverCashSessionId;

            sale.Status = LocalSaleStatus.Completed;
            sale.SyncStatus = SyncQueueStatus.Pending;

            if (sale.SaleDateUtc == default)
                sale.SaleDateUtc = now;

            if (sale.CreatedAtUtc == default)
                sale.CreatedAtUtc = now;

            foreach (var line in sale.Lines)
            {
                if (line.Id == Guid.Empty)
                    line.Id = Guid.NewGuid();

                line.LocalSaleId = sale.Id;
            }

            foreach (var payment in sale.Payments)
            {
                if (payment.Id == Guid.Empty)
                    payment.Id = Guid.NewGuid();

                payment.LocalSaleId = sale.Id;

                if (payment.PaidAtUtc == default)
                    payment.PaidAtUtc = now;

                payment.SyncStatus = SyncQueueStatus.Pending;
            }
        }

        private static void RecalculateSaleTotals(LocalSale sale)
        {
            decimal subtotalExclVat = 0;
            decimal totalVat = 0;
            decimal totalInclVat = 0;

            foreach (var line in sale.Lines)
            {
                if (line.Quantity <= 0)
                    throw new InvalidOperationException($"Invalid quantity for product '{line.ProductName}'.");

                if (line.UnitPrice < 0)
                    throw new InvalidOperationException($"Invalid unit price for product '{line.ProductName}'.");

                if (line.VatRate < 0)
                    throw new InvalidOperationException($"Invalid VAT rate for product '{line.ProductName}'.");

                var grossInclVat = line.Quantity * line.UnitPrice;

                var percentDiscount = grossInclVat * (line.DiscountPercent / 100m);
                var totalDiscount = percentDiscount + line.DiscountAmount;

                if (totalDiscount < 0)
                    totalDiscount = 0;

                if (totalDiscount > grossInclVat)
                    totalDiscount = grossInclVat;

                var netInclVat = grossInclVat - totalDiscount;

                var netExclVat = line.VatRate > 0
                    ? netInclVat / (1 + (line.VatRate / 100m))
                    : netInclVat;

                var vatAmount = netInclVat - netExclVat;

                subtotalExclVat += netExclVat;
                totalVat += vatAmount;
                totalInclVat += netInclVat;
            }

            sale.SubtotalAmount = Math.Round(subtotalExclVat, 2);
            sale.VatAmount = Math.Round(totalVat, 2);
            sale.TotalAmount = Math.Round(totalInclVat, 2);
            sale.DiscountAmount = Math.Round(
                sale.Lines.Sum(x =>
                {
                    var gross = x.Quantity * x.UnitPrice;
                    var percentDiscount = gross * (x.DiscountPercent / 100m);
                    return percentDiscount + x.DiscountAmount;
                }),
                2);

            sale.PaidAmount = Math.Round(sale.Payments.Sum(x => x.Amount), 2);
            sale.ChangeAmount = sale.PaidAmount > sale.TotalAmount
                ? Math.Round(sale.PaidAmount - sale.TotalAmount, 2)
                : 0;

            if (sale.PaidAmount <= 0)
            {
                sale.PaymentStatus = LocalPaymentStatus.Unpaid;
            }
            else if (sale.PaidAmount < sale.TotalAmount)
            {
                sale.PaymentStatus = LocalPaymentStatus.Partial;
            }
            else
            {
                sale.PaymentStatus = LocalPaymentStatus.Paid;
            }
        }

        private static void ValidatePaymentAmount(LocalSale sale)
        {
            if (sale.TotalAmount <= 0)
                throw new InvalidOperationException("Sale total amount must be greater than zero.");

            if (sale.PaidAmount <= 0)
                throw new InvalidOperationException("Paid amount must be greater than zero.");

            if (sale.PaidAmount < sale.TotalAmount)
                throw new InvalidOperationException("Paid amount is lower than sale total. Partial payment is not allowed here yet.");
        }

        private async Task ApplyStockMovementsAsync(LocalSale sale, CancellationToken cancellationToken)
        {
            foreach (var line in sale.Lines)
            {
                var stockProductLocalId = line.UnitProductLocalId != Guid.Empty
                    ? line.UnitProductLocalId
                    : line.ProductLocalId;

                var stockProductServerId = line.UnitProductServerId != Guid.Empty
                    ? line.UnitProductServerId
                    : line.ProductServerId ?? Guid.Empty;

                var stockProductName = !string.IsNullOrWhiteSpace(line.ProductName)
                    ? line.ProductName
                    : "Unknown product";

                var stockQuantityToRemove = CalculateStockQuantityToRemove(line);

                var stock = await _db.Stocks
                    .FirstOrDefaultAsync(x => x.ProductLocalId == stockProductLocalId, cancellationToken);

                if (stock == null)
                {
                    stock = new LocalStock
                    {
                        Id = Guid.NewGuid(),
                        ProductLocalId = stockProductLocalId,
                        ProductServerId = stockProductServerId,
                        ProductName = stockProductName,
                        ProductBarcode = line.ProductBarcode,
                        Quantity = 0,
                        ReservedQuantity = 0,
                        LastUpdatedUtc = DateTime.UtcNow
                    };

                    _db.Stocks.Add(stock);
                }

                var quantityBefore = stock.Quantity;
                var quantityAfter = quantityBefore - stockQuantityToRemove;

                stock.Quantity = quantityAfter;
                stock.LastUpdatedUtc = DateTime.UtcNow;

                var movement = new LocalStockMovement
                {
                    Id = Guid.NewGuid(),
                    ClientOperationId = Guid.NewGuid(),
                    ProductLocalId = stockProductLocalId,
                    ProductServerId = stockProductServerId,
                    ProductName = stockProductName,
                    ProductBarcode = line.ProductBarcode,
                    QuantityChange = -stockQuantityToRemove,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = quantityAfter,
                    Type = LocalStockMovementType.Sale,
                    UnitCost = line.UnitCostPrice,
                    LocalReferenceId = sale.Id,
                    ServerReferenceId = sale.ServerId,
                    ReferenceNumber = sale.LocalInvoiceNumber,
                    Notes = $"Offline sale {sale.LocalInvoiceNumber}",
                    MovementDateUtc = sale.SaleDateUtc,
                    SyncStatus = SyncQueueStatus.Pending
                };

                _db.StockMovements.Add(movement);
            }
        }

        private static decimal CalculateStockQuantityToRemove(LocalSaleLine line)
        {
            if (line.UnitQuantity > 0)
                return line.UnitQuantity;

            if (line.IsPack && line.UnitsPerPack > 0)
                return line.Quantity * line.UnitsPerPack;

            return line.Quantity;
        }

        private async Task CreateCashMovementIfNeededAsync(
            LocalSale sale,
            Guid localCashSessionId,
            CancellationToken cancellationToken)
        {
            var cashPaid = sale.Payments
                .Where(x => string.Equals(x.Method, "Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Amount);

            if (cashPaid <= 0)
                return;

            var cashAmountToDrawer = cashPaid - sale.ChangeAmount;

            if (cashAmountToDrawer <= 0)
                return;

            var movement = new LocalCashMovement
            {
                Id = Guid.NewGuid(),
                ClientOperationId = Guid.NewGuid(),
                LocalCashSessionId = localCashSessionId,
                Type = LocalCashMovementType.Sale,
                Amount = cashAmountToDrawer,
                ReferenceNumber = sale.LocalInvoiceNumber,
                LocalReferenceId = sale.Id,
                ServerReferenceId = sale.ServerId,
                Notes = $"Cash payment for sale {sale.LocalInvoiceNumber}",
                MovementDateUtc = sale.SaleDateUtc,
                SyncStatus = SyncQueueStatus.Pending
            };

            _db.CashMovements.Add(movement);

            await Task.CompletedTask;
        }

        private void CreateSyncQueueItem(LocalSale sale)
        {
            var payloadJson = JsonSerializer.Serialize(sale, new JsonSerializerOptions
            {
                WriteIndented = false,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            });

            var queueItem = new SyncQueueItem
            {
                Id = Guid.NewGuid(),
                LocalEntityId = sale.Id,
                ClientOperationId = sale.ClientOperationId,
                EntityName = SyncEntityName.Sale,
                Operation = SyncOperation.Create,
                PayloadJson = payloadJson,
                Status = SyncQueueStatus.Pending,
                Attempts = 0,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.SyncQueueItems.Add(queueItem);
        }

        private static string GenerateLocalInvoiceNumber(DateTime utcNow)
        {
            return $"LOC-{utcNow:yyyyMMdd-HHmmssfff}";
        }
    }
}
