using Inventory.Dto.Enums;

using System.ComponentModel.DataAnnotations;


namespace Inventory.Dto.Users.Requests
{
    public class UpdateUserRequest
    {
        [MaxLength(200)]
        public string? FullName { get; set; }

        public UserRole? Role { get; set; }
    }
}
