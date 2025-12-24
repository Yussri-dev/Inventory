using Inventory.Services.Features.Auth.Register;
using Inventory.Services.Features.Auth.Login;
using Inventory.Services.Features.Auth.Refresh;
using Inventory.Services.Features.Auth.ChangePassword;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory.Dto.Auth.Results;
using Inventory.Dto.Auth.Requests;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Inventory.Api.Controllers
{
    // Marks this class as a Web API controller
    // Enables automatic binding, validation, and API-specific conventions
    [ApiController]

    // Base route: api/auth
    // [controller] is replaced by the controller name without "Controller"
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // IMediator is the entry point to MediatR
        // Controllers send commands/queries through IMediator
        // Handlers execute the business logic in the Services layer
        private readonly IMediator _mediator;

        // Constructor injection of IMediator
        // IMediator is resolved from the DI container
        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        // HTTP POST: api/auth/register
        [HttpPost("register")]

        // Swagger documentation: response types for this endpoint
        // 200 => returns AuthResult (JWT + expiration)
        [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]

        // 400 => invalid request body (DTO validation errors)
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        // 409 => conflict (e.g. email already registered)
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            // Incoming registration payload from request body
            [FromBody] RegisterRequest request,

            // Cancellation token propagated from the HTTP request
            CancellationToken cancellationToken)
        {
            // ModelState validation ensures DataAnnotations are enforced
            // Keeps invalid DTOs from reaching your business layer
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Send a command to MediatR
            // MediatR routes it to RegisterUserCommandHandler
            var result = await _mediator.Send(
                new RegisterUserCommand(request),
                cancellationToken
            );

            // Return the AuthResult payload (token + expiresAt)
            return Ok(result);
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        // HTTP POST: api/auth/login
        [HttpPost("login")]

        // 200 => returns AuthResult (JWT + expiration)
        [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]

        // 400 => invalid request body
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        // 401 => wrong credentials or authentication failure
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]

        // 403 => authenticated but forbidden (e.g. blocked user scenario)
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login(
            // Incoming login payload from request body
            [FromBody] LoginRequest request,

            // Cancellation token propagated from the HTTP request
            CancellationToken cancellationToken)
        {
            // Validate input before dispatching to MediatR
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Dispatch login command
            // Handler will validate credentials and issue token
            var result = await _mediator.Send(
                new LoginUserCommand(request),
                cancellationToken
            );

            // Return AuthResult (token + expiration)
            return Ok(result);
        }

        /// <summary>
        /// Refresh authentication token
        /// </summary>
        // HTTP POST: api/auth/refresh
        [HttpPost("refresh")]

        // Requires a valid JWT token
        // User identity is read from HttpContext.User
        [Authorize]

        // 200 => returns refreshed AuthResult
        [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]

        // 401 => invalid or missing token
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]

        // 404 => user not found (token subject does not map to user)
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Refresh(
            // Cancellation token propagated from the HTTP request
            CancellationToken cancellationToken)
        {
            // Extract the authenticated user id from claims
            // NameIdentifier is typically the user primary key
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Safety check: if claim missing, treat as unauthorized
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Dispatch refresh command
            // Handler issues a new token for the same user
            var result = await _mediator.Send(
                new RefreshTokenCommand(userId),
                cancellationToken
            );

            // Return refreshed AuthResult
            return Ok(result);
        }

        /// <summary>
        /// Change user password
        /// </summary>
        // HTTP POST: api/auth/change-password
        [HttpPost("change-password")]

        // Requires authentication
        [Authorize]

        // 200 => password changed successfully
        [ProducesResponseType(StatusCodes.Status200OK)]

        // 400 => invalid request body (validation errors)
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        // 401 => missing/invalid authentication
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]

        // 404 => user not found
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword(
            // Incoming payload with current/new passwords
            [FromBody] ChangePasswordRequest request,

            // Cancellation token propagated from the HTTP request
            CancellationToken cancellationToken)
        {
            // Validate DTO with DataAnnotations
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Extract authenticated user id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Reject if claim missing
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Dispatch change-password command
            // Returns Unit because the use-case returns no data payload
            await _mediator.Send(
                new ChangePasswordCommand(
                    userId,
                    request.CurrentPassword,
                    request.NewPassword
                ),
                cancellationToken
            );

            // Return a simple confirmation object
            return Ok(new { message = "Password changed successfully" });
        }

        /// <summary>
        /// Get current user info
        /// </summary>
        // HTTP GET: api/auth/me
        [HttpGet("me")]

        // Requires authentication
        [Authorize]

        // 200 => returns identity info + claims
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]

        // 401 => not authenticated
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            // Extract the user id claim
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Extract the email claim (if present)
            var email = User.FindFirstValue(ClaimTypes.Email);

            // Return identity and full claim list
            // Useful for debugging, profile display, or frontend initialization
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
