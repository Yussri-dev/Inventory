using Inventory.Domain.Entities;
using Inventory.Dto.Enums;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;
using Inventory.Infrastructure.Data;
using Inventory.Services;
using Inventory.Services.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Text.RegularExpressions;

namespace Inventory.Api.Controllers;

[ApiController, Route("api/support"), Authorize(Roles = "Admin,SuperAdmin")]
public sealed class SupportController(InventoryDbContext db, ITenantContext tenant) : ControllerBase
{
    private bool CanAccess(Guid tenantId) => tenantId != Guid.Empty &&
        (tenant.IsSuperAdmin || tenant.TenantId == tenantId);

    [HttpGet("stores")]
    public async Task<IActionResult> Stores(CancellationToken ct) => Ok(await db.Tenants.AsNoTracking()
        .Where(x => tenant.IsSuperAdmin || x.Id == tenant.TenantId)
        .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct));

    [HttpGet("{tenantId:guid}/sales")]
    public async Task<IActionResult> Sales(Guid tenantId, string? search, CancellationToken ct)
    {
        if (!CanAccess(tenantId)) return Forbid();
        var query = db.Sales.AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted && x.Status == SaleStatus.Completed);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.InvoiceNumber.Contains(search));
        return Ok(await query.OrderByDescending(x => x.SaleDate).Take(50).Select(x => new
        {
            x.Id,
            x.InvoiceNumber,
            x.SaleDate,
            x.TotalAmount,
            x.CustomerId,
            x.ModifiedAt,
            DebtToTransfer = db.CustomerTransactions.Where(t => t.TenantId == tenantId && t.SaleId == x.Id &&
                t.CustomerId == x.CustomerId && !t.IsDeleted).Sum(t => t.BalanceAfter - t.BalanceBefore),
            CustomerName = x.Customer != null ? x.Customer.Name : "Sans client"
        }).ToListAsync(ct));
    }

    [HttpGet("{tenantId:guid}/customers")]
    public async Task<IActionResult> Customers(Guid tenantId, string? search, CancellationToken ct)
    {
        if (!CanAccess(tenantId)) return Forbid();
        var query = db.Customers.AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted && x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search));
        return Ok(await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Take(50)
            .Select(x => new { x.Id, x.Name, x.Phone, x.CurrentBalance }).ToListAsync(ct));
    }

    [HttpGet("{tenantId:guid}/corrections")]
    public async Task<IActionResult> History(Guid tenantId, CancellationToken ct)
    {
        if (!CanAccess(tenantId)) return Forbid();
        return Ok(await db.Set<SaleCustomerCorrection>().AsNoTracking().Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt).Take(100).Select(x => new
            {
                x.Id,
                x.SaleId,
                x.Sale.InvoiceNumber,
                x.PreviousCustomerId,
                x.CustomerId,
                x.ActorUserId,
                x.Reason,
                x.TransferredDebt,
                x.CreatedAt
            }).ToListAsync(ct));
    }

    [HttpPost("{tenantId:guid}/sales/{saleId:guid}/customer")]
    public async Task<IActionResult> Correct(Guid tenantId, Guid saleId, CorrectSaleCustomerRequest request, CancellationToken ct)
    {
        if (!CanAccess(tenantId)) return Forbid();
        if (request.OperationId == Guid.Empty || request.CustomerId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { detail = "Le client, le motif et l'identifiant de correction sont obligatoires." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var existing = await db.Set<SaleCustomerCorrection>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == request.OperationId, ct);
            if (existing != null)
            {
                if (existing.SaleId != saleId || existing.CustomerId != request.CustomerId ||
                    existing.PreviousCustomerId != request.ExpectedCustomerId || existing.Reason != request.Reason.Trim())
                    return Conflict(new { detail = "Cet identifiant a déjà été utilisé pour une autre correction." });
                return Ok(new { existing.Id, existing.TransferredDebt });
            }

            var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == saleId && x.TenantId == tenantId && !x.IsDeleted, ct);
            if (sale == null) return NotFound();
            if (sale.Status != SaleStatus.Completed || sale.CustomerId != request.ExpectedCustomerId ||
                sale.ModifiedAt != request.ExpectedModifiedAt)
                return Conflict(new { detail = "La vente a changé. Rechargez-la avant de confirmer." });
            if (sale.CustomerId == request.CustomerId)
                return BadRequest(new { detail = "Ce client est déjà associé à la vente." });
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId &&
                x.TenantId == tenantId && !x.IsDeleted && x.IsActive, ct);
            if (customer == null) return BadRequest(new { detail = "Client introuvable dans ce magasin." });
            if (await db.Returns.AnyAsync(x => x.SaleId == saleId && x.TenantId == tenantId && !x.IsDeleted, ct))
                return Conflict(new { detail = "Cette vente a un retour : sa correction nécessite un rapprochement manuel." });

            var ledger = await db.CustomerTransactions.Where(x => x.TenantId == tenantId && x.SaleId == saleId && !x.IsDeleted).ToListAsync(ct);
            if (ledger.Any(x => x.Type != "Credit" && x.Type != "CorrectionOut" && x.Type != "CorrectionIn"))
                return Conflict(new { detail = "Cette vente a des règlements ou remboursements associés. Un rapprochement est nécessaire." });
            var debt = ledger.Where(x => x.CustomerId == sale.CustomerId).Sum(x => x.BalanceAfter - x.BalanceBefore);
            if (debt != request.ExpectedDebt)
                return Conflict(new { detail = "Le montant de dette a changé. Rechargez la vente pour vérifier la correction." });
            if (debt < 0 || (debt == 0 && sale.PaymentStatus != PaymentStatus.Paid))
                return Conflict(new { detail = "Le crédit de cette vente ne peut pas être établi depuis son historique." });
            Customer? previous = null;
            if (debt > 0)
            {
                previous = await db.Customers.SingleOrDefaultAsync(x => x.Id == sale.CustomerId && x.TenantId == tenantId && !x.IsDeleted, ct);
                if (previous == null || previous.CurrentBalance < debt ||
                    await db.CustomerTransactions.AnyAsync(x => x.TenantId == tenantId && x.CustomerId == previous.Id && !x.IsDeleted &&
                        x.CreatedAt >= sale.CreatedAt && x.SaleId != saleId && (x.Type == "Payment" || x.Type == "Refund"), ct))
                    return Conflict(new { detail = "Des règlements ultérieurs peuvent concerner cette dette. Un rapprochement est nécessaire." });
                if (!customer.AllowCredit || (!customer.HasUnlimitedCredit && customer.CurrentBalance + debt > customer.CreditLimit))
                    return Conflict(new { detail = "Le crédit du nouveau client est désactivé ou sa limite serait dépassée." });
            }

            var now = DateTime.UtcNow;
            var correction = new SaleCustomerCorrection
            {
                Id = request.OperationId,
                TenantId = tenantId,
                SaleId = saleId,
                PreviousCustomerId = sale.CustomerId,
                CustomerId = customer.Id,
                ActorUserId = tenant.UserId,
                Reason = request.Reason.Trim(),
                TransferredDebt = debt,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedByUserId = tenant.UserId
            };
            if (previous != null)
            {
                Transfer(previous, -debt, "CorrectionOut");
                Transfer(customer, debt, "CorrectionIn");
            }
            sale.CustomerId = customer.Id;
            sale.ModifiedAt = now;
            db.Add(correction);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Ok(new { correction.Id, correction.TransferredDebt });

            void Transfer(Customer account, decimal delta, string type)
            {
                var description = $"Correction {correction.Id}: {correction.Reason}";
                db.CustomerTransactions.Add(new CustomerTransaction
                {
                    Id = Guid.NewGuid(),
                    ClientOperationId = Guid.NewGuid(),
                    TenantId = tenantId,
                    SaleId = saleId,
                    CustomerId = account.Id,
                    Type = type,
                    Amount = Math.Abs(delta),
                    BalanceBefore = account.CurrentBalance,
                    BalanceAfter = account.CurrentBalance + delta,
                    Description = description[..Math.Min(500, description.Length)],
                    TransactionDate = now,
                    CreatedAt = now,
                    ModifiedAt = now
                });
                account.CurrentBalance += delta;
                account.ModifiedAt = now;
            }
        }
        catch (Exception ex) when (ex.GetBaseException() is PostgresException { SqlState: "40001" or "23505" or "40P01" })
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return Conflict(new { detail = "Une opération concurrente a été détectée. Rechargez puis réessayez." });
        }
    }

    [HttpGet("catalog-approvals")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CatalogApprovals(
    CancellationToken ct)
    {
        var products =
            await db.Products
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    x.CatalogProductId == null &&
                    x.CatalogApprovalStatus ==
                        CatalogApprovalStatus.Pending)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.TenantId,

                    TenantName =
                        x.Tenant != null
                            ? x.Tenant.Name
                            : null,

                    x.Name,
                    x.Barcode,
                    x.Sku,
                    x.Brand,
                    x.Category,
                    x.Unit,

                    x.CatalogApprovalStatus,
                    x.CreatedAt
                })
                .ToListAsync(ct);

        return Ok(products);
    }


    [HttpPost("catalog-approvals/{productId:guid}/approve")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ApproveCatalogProduct(
    Guid productId,
    ApproveCatalogProductRequest request,
    [FromServices] ProductCatalogService productCatalogService,
    CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var product = await db.Products
            .FirstOrDefaultAsync(
                x =>
                    x.Id == productId &&
                    !x.IsDeleted,
                ct);

        if (product == null)
            return NotFound();

        if (product.CatalogApprovalStatus != CatalogApprovalStatus.Pending)
            return Conflict(new
            {
                detail = "This product is not pending catalog approval."
            });

        if (product.CatalogProductId.HasValue)
            return Conflict(new
            {
                detail = "This product is already linked to the catalog."
            });

        var internalCode = await GenerateInternalCodeAsync(ct);

        var createRequest = new CreateProductCatalogRequest
        {
            Name = product.Name,
            Barcode = product.Barcode,
            InternalCode = internalCode,

            Brand = product.Brand,
            Manufacturer = request.Manufacturer,
            Description = product.Description,

            DefaultSalePrice = product.SalePrice,
            DefaultSalePrice2 = product.SalePrice2,
            DefaultSalePrice3 = product.SalePrice3,
            DefaultPurchasePrice = product.PurchasePrice,
            DefaultVatRate = product.VatRate,

            SellingMode = request.SellingMode,
            UnitOfMeasure = request.UnitOfMeasure,

            CategoryId = request.CategoryId,

            IsPack = false,
            PackComponents = new(),

            CreatedAt = DateTime.UtcNow
        };

        var catalog = await productCatalogService.CreateAsync(
            createRequest,
            product.TenantId);

        product.CatalogProductId = catalog.Id;
        product.CatalogApprovalStatus =
            CatalogApprovalStatus.Approved;

        product.ModifiedAt = DateTime.UtcNow;
        product.ModifiedByUserId = tenant.UserId;

        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Ok(new
        {
            product.Id,
            CatalogProductId = catalog.Id,
            product.CatalogApprovalStatus
        });
    }

    [HttpPost("catalog-approvals/{productId:guid}/reject")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RejectCatalogProduct(
    Guid productId,
    CancellationToken ct)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(
                x =>
                    x.Id == productId &&
                    !x.IsDeleted,
                ct);

        if (product == null)
            return NotFound();

        if (product.CatalogApprovalStatus != CatalogApprovalStatus.Pending)
        {
            return Conflict(new
            {
                detail = "This product is not pending catalog approval."
            });
        }

        if (product.CatalogProductId.HasValue)
        {
            return Conflict(new
            {
                detail = "This product is already linked to the catalog."
            });
        }

        product.CatalogApprovalStatus =
            CatalogApprovalStatus.Rejected;

        product.ModifiedAt = DateTime.UtcNow;
        product.ModifiedByUserId = tenant.UserId;

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            product.Id,
            product.CatalogApprovalStatus
        });
    }

    [HttpGet("catalog-categories")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CatalogCategories(
    CancellationToken ct)
    {
        var categories = await db.ProductCategories
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name
            })
            .ToListAsync(ct);

        return Ok(categories);
    }

    private async Task<string> GenerateInternalCodeAsync(
     CancellationToken cancellationToken)
    {
        var lastNumericCode = await db.ProductCatalogs
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.InternalCode != null &&
                Regex.IsMatch(
                    x.InternalCode,
                    @"^[0-9]+$"))
            .OrderByDescending(x =>
                x.InternalCode!.Length)
            .ThenByDescending(x =>
                x.InternalCode)
            .Select(x =>
                x.InternalCode)
            .FirstOrDefaultAsync(cancellationToken);

        long nextNumber = 1;

        if (!string.IsNullOrWhiteSpace(lastNumericCode) &&
            long.TryParse(
                lastNumericCode,
                out var lastNumber))
        {
            nextNumber = lastNumber + 1;
        }

        return nextNumber.ToString("D6");
    }

}

[ApiController, Route("api/sync/sale-customers"), Authorize]
public sealed class SaleCustomerSyncController(InventoryDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Snapshot([FromBody] Guid[] saleIds, CancellationToken ct)
    {
        if (saleIds.Length > 250) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var sales = await db.Sales.AsNoTracking().Where(x => x.TenantId == tenant.TenantId &&
            !x.IsDeleted && saleIds.Contains(x.Id)).Select(x => new SaleCustomerSnapshot
            {
                SaleId = x.Id,
                CustomerId = x.CustomerId
            }).ToListAsync(ct);
        var entries = await db.CustomerTransactions.AsNoTracking().Where(x => x.TenantId == tenant.TenantId &&
            !x.IsDeleted && x.SaleId.HasValue && saleIds.Contains(x.SaleId.Value) &&
            (x.Type == "CorrectionIn" || x.Type == "CorrectionOut"))
            .Select(x => new CorrectionLedgerEntry
            {
                Id = x.Id,
                OperationId = x.ClientOperationId,
                CustomerId = x.CustomerId,
                SaleId = x.SaleId!.Value,
                Type = x.Type,
                Amount = x.Amount,
                BalanceBefore = x.BalanceBefore,
                BalanceAfter = x.BalanceAfter,
                Description = x.Description,
                Date = x.TransactionDate
            }).ToListAsync(ct);
        var ids = sales.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value)
            .Concat(entries.Select(x => x.CustomerId)).Distinct().ToArray();
        var customers = await db.Customers.AsNoTracking().Where(x => x.TenantId == tenant.TenantId && ids.Contains(x.Id))
            .Select(x => new CorrectedCustomerBalance
            {
                CustomerId = x.Id,
                Balance = x.CurrentBalance,
                Name = x.Name,
                Phone = x.Phone,
                Email = x.Email,
                Address = x.Address,
                TaxNumber = x.TaxNumber,
                Notes = x.Notes,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                AllowCredit = x.AllowCredit,
                HasUnlimitedCredit = x.HasUnlimitedCredit,
                CreditLimit = x.CreditLimit
            }).ToListAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new SaleCustomerSyncResult { Sales = sales, Customers = customers, Entries = entries });
    }
}


