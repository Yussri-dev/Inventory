// Base .NET namespaces
// Included by default for consistency across DTO files
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Auth.Requests
{
    // DTO used when a user requests a password change
    // This object is validated automatically by ASP.NET ModelState
    public class ChangePasswordRequest
    {
        // The user's current password
        // Required to verify identity before allowing a password change
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        // The new password the user wants to set
        // Must meet minimum length requirements
        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string NewPassword { get; set; } = string.Empty;

        // Confirmation of the new password
        // Must match the NewPassword field exactly
        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
