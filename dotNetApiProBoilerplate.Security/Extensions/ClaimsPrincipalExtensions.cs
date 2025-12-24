// JWT claim constants (e.g. "sub", "email", etc.)
using System.IdentityModel.Tokens.Jwt;

// ClaimsPrincipal represents the authenticated user (HttpContext.User)
using System.Security.Claims;

namespace Inventory.Security.Extensions
{
    // Extension methods for ClaimsPrincipal
    // Adds convenience helpers for extracting common identity data
    public static class ClaimsPrincipalExtensions
    {
        // Retrieves the authenticated user's ID from the JWT claims
        //
        // Design notes:
        // - Uses JwtRegisteredClaimNames.Sub ("sub") as the user identifier
        // - Matches how JwtTokenGenerator embeds the userId
        // - Centralizes claim parsing logic in one place
        //
        // Why this should NOT be deleted:
        // - Avoids duplicated claim-parsing logic across controllers/services
        // - Prevents magic strings ("sub") scattered in the codebase
        // - Improves readability: User.GetUserId() is explicit and intention-revealing
        // - Makes future changes trivial (e.g. switch to NameIdentifier)
        //
        // Why it returns Guid?:
        // - JWT claims are strings
        // - Domain logic usually expects strongly typed identifiers
        // - Null is returned if:
        //   - User is not authenticated
        //   - Claim is missing
        //   - Claim value is not a valid Guid
        public static Guid? GetUserId(this ClaimsPrincipal principal)
        {
            // Attempt to retrieve the "sub" (subject) claim from the JWT
            var claim = principal?.FindFirst(JwtRegisteredClaimNames.Sub);

            // Safely parse the claim value into a Guid
            // Returns null if parsing fails
            return Guid.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
