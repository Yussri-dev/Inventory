using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Auth.Requests
{
    public class RegisterUserRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(200, ErrorMessage = "Full name cannot exceed 200 characters")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Tenant ID is required")]
        public Guid TenantId { get; set; }

        [Required(ErrorMessage = "User role is required")]
        public UserRole Role { get; set; }

        public Guid CreatedByUserId { get; set; }
    }

}
