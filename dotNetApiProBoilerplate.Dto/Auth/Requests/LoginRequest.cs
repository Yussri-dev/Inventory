namespace Inventory.Dto.Auth.Requests
{
    // DTO used for user authentication (login)
    // Represents the minimal credentials required to obtain a JWT token
    public class LoginRequest
    {
        // User email address
        // Marked as required at compile time (C# 11 required members)
        // Enforced during object initialization
        public required string Email { get; init; }

        // User password
        // Also marked as required to prevent incomplete request objects
        // Validation of correctness happens in the service layer
        public required string Password { get; init; }
    }
}
