using MediatR;

namespace Inventory.Services.Features.Auth.ChangePassword
{
    public class ChangePasswordCommand : IRequest<Unit>
    {
        public string UserId { get; }
        public string CurrentPassword { get; }
        public string NewPassword { get; }

        public ChangePasswordCommand(string userId, string currentPassword, string newPassword)
        {
            UserId = userId;
            CurrentPassword = currentPassword;
            NewPassword = newPassword;
        }
    }
}
