using Inventory.Domain.Models;
using Inventory.Dto.Auth.Requests;
using Inventory.Dto.Auth.Results;
using Inventory.Dto.Enums;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Identity;
using Inventory.Services.Abstractions;
using Inventory.Services.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security;

namespace Inventory.Services
{
    public sealed class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtTokenGenerator _jwt;
        private readonly InventoryDbContext _context;
        private readonly IProductProvisioningService _productProvisioningService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            JwtTokenGenerator jwt,
            InventoryDbContext context,
            IProductProvisioningService productProvisioningService)
        {
            _userManager = userManager;
            _jwt = jwt;
            _context = context;
            _productProvisioningService = productProvisioningService;
        }

        /// <summary>
        /// Registers a new company, creates its tenant and admin user,
        /// then provisions products from the global product catalog.
        /// </summary>
        public async Task<AuthResult> RegisterCompanyAsync(
            RegisterCompanyRequest request,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = request.Email.Trim();
            var now = DateTime.UtcNow;
            var limits = SubscriptionPlanResolver.GetLimits(request.SubscriptionPlan);

            var existingUser =
                await _userManager.FindByEmailAsync(normalizedEmail);

            if (existingUser is not null)
            {
                throw new ConflictException(
                    $"User with email '{normalizedEmail}' already exists.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),

                    Name = request.CompanyName.Trim(),
                    Email = request.Email.Trim(),

                    IsActive = true,
                    CreatedAt = now,
                    CreatedByUserId = Guid.Empty,

                    Currency = "EUR",
                    Locale = "fr-BE",
                    TimeZone = "Europe/Brussels",
                    DateFormat = "dd/MM/yyyy",
                    TimeFormat = "HH:mm",
                    DecimalPlaces = 2,
                    DefaultVatRate = 21m,

                    SubscriptionPlan = request.SubscriptionPlan.ToString(),

                    IsTrialActive = request.EnableTrial,

                    TrialEndDate = request.EnableTrial ? now.AddDays(30) : null,

                    MaxUsers = limits.MaxUsers,

                    MaxProducts = limits.MaxProducts,

                    MaxLocations = limits.MaxLocations,

                    MaxMonthlyTransactions = limits.MaxMonthlyTransactions,

                    CurrentUsers = 0,
                    CurrentProducts = 0,
                    CurrentLocations = 0,
                    CurrentMonthTransactions = 0,

                    LastTransactionCountReset = now
                };

                await _context.Tenants.AddAsync(
                    tenant,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                var user = new ApplicationUser
                {
                    UserName = normalizedEmail,
                    Email = normalizedEmail,
                    FullName = request.FullName.Trim(),

                    TenantId = tenant.Id,
                    Role = UserRole.Admin,

                    IsActive = true,
                    CreatedAt = now,

                    PreferredLanguage = "en",
                    PreferredTheme = "light"
                };

                var identityResult = await _userManager.CreateAsync(
                    user,
                    request.Password);

                if (!identityResult.Succeeded)
                {
                    throw CreateIdentityValidationException(
                        identityResult);
                }

                tenant.CreatedByUserId = user.Id;
                tenant.CurrentUsers = 1;

                var importedProductsCount =
                    await _productProvisioningService
                        .ProvisionCatalogProductsAsync(
                            tenant.Id,
                            user.Id,
                            cancellationToken);

                //tenant.CurrentProducts = importedProductsCount;

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return GenerateResult(user);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Registers a new user inside an existing tenant.
        /// </summary>
        public async Task<AuthResult> RegisterUserAsync(
            RegisterUserRequest request,
            CancellationToken cancellationToken = default)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(
                    x => x.Id == request.TenantId,
                    cancellationToken);

            if (tenant is null)
            {
                throw new NotFoundException(
                    "Tenant",
                    request.TenantId.ToString());
            }

            if (tenant.CurrentUsers >= tenant.MaxUsers)
            {
                throw new ForbiddenException(
                    $"Tenant has reached maximum user limit ({tenant.MaxUsers}).");
            }

            var normalizedEmail = request.Email.Trim();

            var existingUser =
                await _userManager.FindByEmailAsync(normalizedEmail);

            if (existingUser is not null)
            {
                throw new ConflictException(
                    $"User with email '{normalizedEmail}' already exists.");
            }

            var user = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                FullName = request.FullName.Trim(),

                TenantId = request.TenantId,
                Role = request.Role,

                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = request.CreatedByUserId,

                PreferredLanguage = "en",
                PreferredTheme = "light"
            };

            var identityResult = await _userManager.CreateAsync(
                user,
                request.Password);

            if (!identityResult.Succeeded)
            {
                throw CreateIdentityValidationException(
                    identityResult);
            }

            tenant.CurrentUsers++;

            await _context.SaveChangesAsync(cancellationToken);

            return GenerateResult(user);
        }

        public async Task<AuthResult> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(
                request.Email.Trim());

            if (user is null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");
            }

            if (user.Role != UserRole.SuperAdmin &&
                user.TenantId is null)
            {
                throw new SecurityException(
                    "User without tenant is invalid.");
            }

            if (!user.IsActive)
            {
                throw new ForbiddenException(
                    "Account is deactivated.");
            }

            if (user.Role != UserRole.SuperAdmin)
            {
                var tenant = await _context.Tenants
                    .FirstOrDefaultAsync(
                        x => x.Id == user.TenantId,
                        cancellationToken);

                if (tenant is null || !tenant.IsActive)
                {
                    throw new ForbiddenException(
                        "Company account is not active.");
                }

                if (!tenant.IsSubscriptionActive())
                {
                    throw new ForbiddenException(
                        "Company subscription has expired.");
                }

                tenant.LastActivityAt = DateTime.UtcNow;
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new ForbiddenException(
                    "Account is locked. Please try again later.");
            }

            var validPassword =
                await _userManager.CheckPasswordAsync(
                    user,
                    request.Password);

            if (!validPassword)
            {
                await _userManager.AccessFailedAsync(user);

                throw new UnauthorizedAccessException(
                    "Invalid email or password.");
            }

            if (await _userManager.GetAccessFailedCountAsync(user) > 0)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = request.IpAddress;

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                throw CreateIdentityValidationException(
                    updateResult);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (user.Role != UserRole.SuperAdmin &&
                user.TenantId.HasValue &&
                user.TenantId.Value != Guid.Empty)
            {
                await _productProvisioningService.ProvisionCatalogProductsAsync(
                    user.TenantId.Value,
                    user.Id,
                    cancellationToken);
            }

            return GenerateResult(user);
        }

        public async Task<AuthResult> RefreshTokenAsync(
            string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new NotFoundException(
                    "User",
                    userId);
            }

            if (!user.IsActive)
            {
                throw new ForbiddenException(
                    "Account is deactivated.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new ForbiddenException(
                    "Account is locked.");
            }

            return GenerateResult(user);
        }

        public async Task ChangePasswordAsync(
            string userId,
            string currentPassword,
            string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new NotFoundException(
                    "User",
                    userId);
            }

            var result =
                await _userManager.ChangePasswordAsync(
                    user,
                    currentPassword,
                    newPassword);

            if (!result.Succeeded)
            {
                throw CreateIdentityValidationException(result);
            }

            user.PasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = false;

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                throw CreateIdentityValidationException(
                    updateResult);
            }
        }

        private AuthResult GenerateResult(ApplicationUser user)
        {
            var tenant = user.TenantId.HasValue
                ? _context.Tenants.FirstOrDefault(
                    x => x.Id == user.TenantId.Value)
                : null;

            Guid? tenantIdForToken =
                user.Role == UserRole.SuperAdmin
                    ? null
                    : user.TenantId;

            return new AuthResult
            {
                AccessToken = _jwt.Generate(
                    user.Id,
                    user.Email!,
                    tenantIdForToken,
                    user.Role.ToString()),

                ExpiresAt = DateTime.UtcNow.AddHours(2),

                UserId = user.Id,
                Email = user.Email!,
                TenantId = user.TenantId,
                FullName = user.FullName,
                Role = user.Role.ToString(),

                TrialEndDate = tenant?.TrialEndDate,
                IsTrialActive = tenant?.IsTrialActive ?? false
            };
        }

        private static ValidationException
            CreateIdentityValidationException(
                IdentityResult identityResult)
        {
            var groupedErrors =
                new Dictionary<string, List<string>>();

            foreach (var error in identityResult.Errors)
            {
                var key = error.Code switch
                {
                    var code when code.Contains(
                        "Password",
                        StringComparison.OrdinalIgnoreCase)
                        => "Password",

                    var code when code.Contains(
                        "Email",
                        StringComparison.OrdinalIgnoreCase)
                        => "Email",

                    var code when code.Contains(
                        "UserName",
                        StringComparison.OrdinalIgnoreCase)
                        => "Email",

                    _ => "General"
                };

                if (!groupedErrors.TryGetValue(
                    key,
                    out var messages))
                {
                    messages = new List<string>();
                    groupedErrors[key] = messages;
                }

                messages.Add(error.Description);
            }

            return new ValidationException(
                groupedErrors.ToDictionary(
                    x => x.Key,
                    x => x.Value.ToArray()));
        }
    }
}