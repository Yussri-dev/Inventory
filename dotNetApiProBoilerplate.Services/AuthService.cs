using Inventory.Domain.Models;
using Inventory.Dto.Auth.Requests;
using Inventory.Dto.Auth.Results;
using Inventory.Infrastructure.Identity;
using Inventory.Services.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Inventory.Services
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtTokenGenerator _jwt;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            JwtTokenGenerator jwt)
        {
            _userManager = userManager;
            _jwt = jwt;
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ConflictException($"User with email '{request.Email}' already exists.");
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                // Convert Identity errors to ValidationException
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

            return GenerateResult(user);
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                // Don't reveal whether user exists or not for security
                throw new UnauthorizedAccessException("Invalid email or password.");
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
                // Increment failed login attempts
                await _userManager.AccessFailedAsync(user);

                // Don't reveal whether user exists or not for security
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // Reset failed login attempts on successful login
            if (await _userManager.GetAccessFailedCountAsync(user) > 0)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            return GenerateResult(user);
        }

        public async Task<AuthResult> RefreshTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("User", userId);
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
        }

        private AuthResult GenerateResult(ApplicationUser user)
        {
            return new AuthResult
            {
                Token = _jwt.Generate(user.Id, user.Email!),
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };
        }
    }
}