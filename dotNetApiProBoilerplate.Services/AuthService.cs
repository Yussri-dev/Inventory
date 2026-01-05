using Inventory.Domain.Models;
using Inventory.Dto.Auth.Requests;
using Inventory.Dto.Auth.Results;
using Inventory.Dto.Enums;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Identity;
using Inventory.Services.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtTokenGenerator _jwt;
        private readonly InventoryDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            JwtTokenGenerator jwt,
            InventoryDbContext context)
        {
            _userManager = userManager;
            _jwt = jwt;
            _context = context;
        }

        /// <summary>
        /// Register a new company owner - creates a new tenant
        /// </summary>
        public async Task<AuthResult> RegisterCompanyAsync(RegisterCompanyRequest request)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ConflictException($"User with email '{request.Email}' already exists.");
            }

            // Create tenant for the company
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.CompanyName,
                Email = request.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = Guid.Empty, // Will be updated after user is created
                Currency = "EUR",
                Locale = "fr-BE",
                TimeZone = "Europe/Brussels",
                DateFormat = "dd/MM/yyyy",
                TimeFormat = "HH:mm",
                DecimalPlaces = 2,
                DefaultVatRate = 21.00m,
                SubscriptionPlan = "Free",
                IsTrialActive = true,
                TrialEndDate = DateTime.UtcNow.AddDays(30),
                MaxUsers = 5,
                MaxProducts = 1000,
                MaxLocations = 1,
                MaxMonthlyTransactions = 10000,
                CurrentUsers = 0,
                CurrentProducts = 0,
                CurrentLocations = 0,
                CurrentMonthTransactions = 0,
                LastTransactionCountReset = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // Create company owner user (Admin role)
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                TenantId = tenant.Id,
                Role = UserRole.Admin, // Owner is always Admin
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PreferredLanguage = "en",
                PreferredTheme = "light"
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                // Rollback: Delete the tenant if user creation fails
                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync();

                var errors = new Dictionary<string, string[]>();
                foreach (var error in result.Errors)
                {
                    var key = error.Code switch
                    {
                        var code when code.Contains("Password") => "Password",
                        var code when code.Contains("Email") => "Email",
                        var code when code.Contains("UserName") => "Email",
                        _ => "General"
                    };

                    if (errors.ContainsKey(key))
                    {
                        var existingErrors = errors[key].ToList();
                        existingErrors.Add(error.Description);
                        errors[key] = existingErrors.ToArray();
                    }
                    else
                    {
                        errors[key] = new[] { error.Description };
                    }
                }

                throw new ValidationException(errors);
            }

            // Update tenant with the actual creator user ID
            tenant.CreatedByUserId = user.Id;
            tenant.CurrentUsers = 1;
            await _context.SaveChangesAsync();

            return GenerateResult(user);
        }

        /// <summary>
        /// Register a new user to an existing tenant (invited by admin)
        /// </summary>
        public async Task<AuthResult> RegisterUserAsync(RegisterUserRequest request)
        {
            // Verify tenant exists
            var tenant = await _context.Tenants.FindAsync(request.TenantId);
            if (tenant == null)
            {
                throw new NotFoundException("Tenant", request.TenantId.ToString());
            }

            // Check if tenant can add more users
            if (tenant.CurrentUsers >= tenant.MaxUsers)
            {
                throw new ForbiddenException($"Tenant has reached maximum user limit ({tenant.MaxUsers}).");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ConflictException($"User with email '{request.Email}' already exists.");
            }

            // Create new user for existing tenant
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                TenantId = request.TenantId,
                Role = request.Role, // Manager, Cashier, User, etc.
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = request.CreatedByUserId, // Admin who invited them
                PreferredLanguage = "en",
                PreferredTheme = "light"
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = new Dictionary<string, string[]>();
                foreach (var error in result.Errors)
                {
                    var key = error.Code switch
                    {
                        var code when code.Contains("Password") => "Password",
                        var code when code.Contains("Email") => "Email",
                        var code when code.Contains("UserName") => "Email",
                        _ => "General"
                    };

                    if (errors.ContainsKey(key))
                    {
                        var existingErrors = errors[key].ToList();
                        existingErrors.Add(error.Description);
                        errors[key] = existingErrors.ToArray();
                    }
                    else
                    {
                        errors[key] = new[] { error.Description };
                    }
                }

                throw new ValidationException(errors);
            }

            // Update tenant user count
            tenant.CurrentUsers++;
            await _context.SaveChangesAsync();

            return GenerateResult(user);
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                throw new ForbiddenException("Account is deactivated.");
            }

            // Check if tenant is active
            var tenant = await _context.Tenants.FindAsync(user.TenantId);
            if (tenant == null || !tenant.IsActive)
            {
                throw new ForbiddenException("Company account is not active.");
            }

            // Check if tenant subscription is active
            if (!tenant.IsSubscriptionActive())
            {
                throw new ForbiddenException("Company subscription has expired.");
            }

            // Check if account is locked
            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new ForbiddenException("Account is locked. Please try again later.");
            }

            // Validate password
            var valid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!valid)
            {
                await _userManager.AccessFailedAsync(user);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // Reset failed login attempts on successful login
            if (await _userManager.GetAccessFailedCountAsync(user) > 0)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            // Update last login info
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = request.IpAddress;
            await _userManager.UpdateAsync(user);

            // Update tenant last activity
            tenant.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return GenerateResult(user);
        }

        public async Task<AuthResult> RefreshTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            if (!user.IsActive)
            {
                throw new ForbiddenException("Account is deactivated.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new ForbiddenException("Account is locked.");
            }

            return GenerateResult(user);
        }

        public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Password", result.Errors.Select(e => e.Description).ToArray() }
                };
                throw new ValidationException(errors);
            }

            user.PasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
        }

        private AuthResult GenerateResult(ApplicationUser user)
        {
            return new AuthResult
            {
                Token = _jwt.Generate(user.Id, user.Email!, user.TenantId, user.Role.ToString()),
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                UserId = user.Id,
                Email = user.Email!,
                TenantId = user.TenantId,
                FullName = user.FullName,
                Role = user.Role.ToString()
            };
        }
    }
}