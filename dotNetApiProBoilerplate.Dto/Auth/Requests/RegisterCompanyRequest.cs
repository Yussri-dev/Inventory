using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Auth.Requests
{
    public class RegisterCompanyRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Company name is required")]
        [MaxLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
        public string CompanyName { get; set; } = null!;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(200, ErrorMessage = "Full name cannot exceed 200 characters")]
        public string FullName { get; set; } = null!;
    }


}
