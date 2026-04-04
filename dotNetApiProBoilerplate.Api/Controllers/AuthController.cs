using Inventory.Dto.Auth.Requests;
using Inventory.Dto.Auth.Results;
using Inventory.Dto.Enums;
using Inventory.Services.Features.Auth.ChangePassword;
using Inventory.Services.Features.Auth.Login;
using Inventory.Services.Features.Auth.Refresh;
using Inventory.Services.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Register a new company (creates a new tenant and admin user)
        /// </summary>
        [HttpPost("register/company")]
        [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegisterCompany(
            [FromBody] RegisterCompanyRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(
                new RegisterCompanyCommand(request),
                cancellationToken
            );

            return Ok(result);
        }

        /// <summary>
        /// Register a new user to an existing company (Admin only)
        /// </summary>
        //[HttpPost("register/user")]
        //[Authorize(Roles = "Admin,SuperAdmin")]
        //[ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status409Conflict)]
        //public async Task<IActionResult> RegisterUser(
        //    [FromBody] RegisterUserRequest request,
        //    CancellationToken cancellationToken)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    // Get current admin user ID and tenant ID from token
        //    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var tenantId = User.FindFirstValue("TenantId");

        //    if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(tenantId))
        //        return Unauthorized();

        //    // Auto-set tenant ID and creator from admin's token
        //    request.TenantId = Guid.Parse(tenantId);
        //    request.CreatedByUserId = Guid.Parse(currentUserId);

        //    var result = await _mediator.Send(
        //        new RegisterUserCommand(request),
        //        cancellationToken
        //    );

        //    return Ok(result);
        //}

        [HttpPost("register/user")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegisterUser(
    [FromBody] RegisterUserRequest request,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tenantId = User.FindFirstValue("TenantId");
            var currentRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            // Rôles qu'un Admin peut créer
            var allowedRoles = new[] { UserRole.Manager, UserRole.Cashier, UserRole.StockManager, UserRole.Viewer };

            if (currentRole == "Admin" && !allowedRoles.Contains(request.Role))
                return Forbid(); // Ne peut pas créer Admin ou SuperAdmin

            request.TenantId = Guid.Parse(tenantId!);
            request.CreatedByUserId = Guid.Parse(currentUserId);

            var result = await _mediator.Send(new RegisterUserCommand(request), cancellationToken);

            return Ok(result);
        }


        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Capture IP address
            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _mediator.Send(
                new LoginUserCommand(request),
                cancellationToken
            );

            return Ok(result);
        }

        /// <summary>
        /// Refresh authentication token
        /// </summary>
        [HttpPost("refresh")]
        [Authorize]
        [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Refresh(
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _mediator.Send(
                new RefreshTokenCommand(userId),
                cancellationToken
            );

            return Ok(result);
        }

        /// <summary>
        /// Change user password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _mediator.Send(
                new ChangePasswordCommand(
                    userId,
                    request.CurrentPassword,
                    request.NewPassword
                ),
                cancellationToken
            );

            return Ok(new { message = "Password changed successfully" });
        }

        /// <summary>
        /// Get current user info
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);

            return Ok(new
            {
                userId,
                email,
                claims = User.Claims.Select(c => new
                {
                    c.Type,
                    c.Value
                })
            });
        }


    }

   
}