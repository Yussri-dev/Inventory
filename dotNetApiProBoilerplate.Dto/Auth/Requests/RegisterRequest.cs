namespace Inventory.Dto.Auth.Requests
{
    // DTO used when registering a new user
    // This object represents the minimal data required to create an account
    public class RegisterRequest
    {
        // Email address of the user
        // Marked as required using C# 11 required members
        // Ensures the property must be provided during object initialization
        public required string Email { get; init; }

        // Plain-text password provided during registration
        // Marked as required to prevent incomplete registration requests
        // Password strength rules are enforced in the Identity configuration
        public required string Password { get; init; }
    }


}
