using Inventory.Dto.Tenants.Requests;
using Inventory.Dto.Tenants.Results;
using Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class TenantController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public TenantController(InventoryDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get current user's company/tenant information
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyTenant()
        {
            // Get tenant ID from JWT token
            var tenantId = User.FindFirstValue("TenantId");

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized(new { message = "TenantId not found in token" });

            var tenant = await _context.Tenants
                .Where(t => t.Id == Guid.Parse(tenantId))
                .Select(t => new TenantResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    LegalName = t.LegalName,
                    Email = t.Email,
                    Phone = t.Phone,
                    Address = t.Address,
                    City = t.City,
                    PostalCode = t.PostalCode,
                    Country = t.Country,
                    TaxNumber = t.TaxNumber,

                    // Regional Settings
                    Currency = t.Currency,
                    CurrencySymbol = t.CurrencySymbol,
                    Locale = t.Locale,
                    TimeZone = t.TimeZone,
                    DateFormat = t.DateFormat,
                    TimeFormat = t.TimeFormat,
                    DefaultVatRate = t.DefaultVatRate,

                    // Subscription Info
                    SubscriptionPlan = t.SubscriptionPlan,
                    SubscriptionStartDate = t.SubscriptionStartDate,
                    SubscriptionEndDate = t.SubscriptionEndDate,
                    IsTrialActive = t.IsTrialActive,
                    TrialEndDate = t.TrialEndDate,

                    // Limits
                    MaxUsers = t.MaxUsers,
                    MaxProducts = t.MaxProducts,
                    MaxLocations = t.MaxLocations,
                    MaxMonthlyTransactions = t.MaxMonthlyTransactions,

                    // Current Usage
                    CurrentUsers = t.CurrentUsers,
                    CurrentProducts = t.CurrentProducts,
                    CurrentLocations = t.CurrentLocations,
                    CurrentMonthTransactions = t.CurrentMonthTransactions,

                    // Status
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    LastActivityAt = t.LastActivityAt
                })
                .FirstOrDefaultAsync();

            if (tenant == null)
                return NotFound(new { message = "Tenant not found" });

            return Ok(tenant);
        }

        /// <summary>
        /// Get tenant by ID (SuperAdmin only or own tenant)
        /// </summary>
        [HttpGet("{tenantId:guid}")]
        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTenantById(Guid tenantId)
        {
            var currentTenantId = User.FindFirstValue("TenantId");
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Only SuperAdmin can view other tenants
            if (userRole != "SuperAdmin" && currentTenantId != tenantId.ToString())
            {
                return Forbid();
            }

            var tenant = await _context.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => new TenantResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    LegalName = t.LegalName,
                    Email = t.Email,
                    Phone = t.Phone,
                    Address = t.Address,
                    City = t.City,
                    PostalCode = t.PostalCode,
                    Country = t.Country,
                    TaxNumber = t.TaxNumber,
                    Currency = t.Currency,
                    CurrencySymbol = t.CurrencySymbol,
                    Locale = t.Locale,
                    TimeZone = t.TimeZone,
                    DateFormat = t.DateFormat,
                    TimeFormat = t.TimeFormat,
                    DefaultVatRate = t.DefaultVatRate,
                    SubscriptionPlan = t.SubscriptionPlan,
                    SubscriptionStartDate = t.SubscriptionStartDate,
                    SubscriptionEndDate = t.SubscriptionEndDate,
                    IsTrialActive = t.IsTrialActive,
                    TrialEndDate = t.TrialEndDate,
                    MaxUsers = t.MaxUsers,
                    MaxProducts = t.MaxProducts,
                    MaxLocations = t.MaxLocations,
                    MaxMonthlyTransactions = t.MaxMonthlyTransactions,
                    CurrentUsers = t.CurrentUsers,
                    CurrentProducts = t.CurrentProducts,
                    CurrentLocations = t.CurrentLocations,
                    CurrentMonthTransactions = t.CurrentMonthTransactions,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    LastActivityAt = t.LastActivityAt
                })
                .FirstOrDefaultAsync();

            if (tenant == null)
                return NotFound(new { message = "Tenant not found" });

            return Ok(tenant);
        }

        /// <summary>
        /// Update current tenant information (Admin only)
        /// </summary>
        [HttpPut("me")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMyTenant([FromBody] UpdateTenantRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenantId = User.FindFirstValue("TenantId");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized();

            var tenant = await _context.Tenants.FindAsync(Guid.Parse(tenantId));

            if (tenant == null)
                return NotFound(new { message = "Tenant not found" });

            // Update tenant information
            tenant.Name = request.Name ?? tenant.Name;
            tenant.LegalName = request.LegalName ?? tenant.LegalName;
            tenant.Email = request.Email ?? tenant.Email;
            tenant.Phone = request.Phone ?? tenant.Phone;
            tenant.Address = request.Address ?? tenant.Address;
            tenant.City = request.City ?? tenant.City;
            tenant.PostalCode = request.PostalCode ?? tenant.PostalCode;
            tenant.Country = request.Country ?? tenant.Country;
            tenant.TaxNumber = request.TaxNumber ?? tenant.TaxNumber;
            tenant.DefaultVatRate = request.DefaultVatRate ?? tenant.DefaultVatRate;

            tenant.UpdatedAt = DateTime.UtcNow;
            tenant.UpdatedByUserId = Guid.Parse(userId!);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Tenant updated successfully" });
        }

        /// <summary>
        /// Get all users in current tenant (Admin only)
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(List<TenantUserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTenantUsers()
        {
            var tenantId = User.FindFirstValue("TenantId");

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized();

            var users = await _context.Users
                .Where(u => u.TenantId == Guid.Parse(tenantId))
                .Select(u => new TenantUserResponse
                {
                    Id = u.Id,
                    Email = u.Email!,
                    FullName = u.FullName,
                    Role = u.Role.ToString(),
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Get tenant statistics (Admin only)
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(TenantStatsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTenantStats()
        {
            var tenantId = User.FindFirstValue("TenantId");

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized();

            var tenantGuid = Guid.Parse(tenantId);

            var stats = new TenantStatsResponse
            {
                TotalUsers = await _context.Users.CountAsync(u => u.TenantId == tenantGuid),
                ActiveUsers = await _context.Users.CountAsync(u => u.TenantId == tenantGuid && u.IsActive),
                TotalProducts = await _context.Products.CountAsync(p => p.TenantId == tenantGuid),
                TotalCustomers = await _context.Customers.CountAsync(c => c.TenantId == tenantGuid),
                TotalSuppliers = await _context.Suppliers.CountAsync(s => s.TenantId == tenantGuid),
                TotalSalesThisMonth = await _context.Sales
                    .Where(s => s.TenantId == tenantGuid &&
                                s.SaleDate.Month == DateTime.UtcNow.Month &&
                                s.SaleDate.Year == DateTime.UtcNow.Year)
                    .CountAsync(),
                TotalRevenueThisMonth = await _context.Sales
                    .Where(s => s.TenantId == tenantGuid &&
                                s.SaleDate.Month == DateTime.UtcNow.Month &&
                                s.SaleDate.Year == DateTime.UtcNow.Year)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0
            };

            return Ok(stats);
        }

        /// <summary>
        /// Check if tenant can add more users
        /// </summary>
        [HttpGet("can-add-user")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> CanAddUser()
        {
            var tenantId = User.FindFirstValue("TenantId");

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized();

            var tenant = await _context.Tenants.FindAsync(Guid.Parse(tenantId));

            if (tenant == null)
                return NotFound();

            return Ok(new
            {
                canAdd = tenant.CurrentUsers < tenant.MaxUsers,
                currentUsers = tenant.CurrentUsers,
                maxUsers = tenant.MaxUsers,
                availableSlots = tenant.MaxUsers - tenant.CurrentUsers
            });
        }
    }
}