using Inventory.Dto.Auth.Requests;
using Inventory.Dto.Auth.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IAuthApi
    {
        [Post("/api/Auth/refresh")]
        Task<AuthResult> Refresh([Body] RefreshTokenRequest request);

        [Post("/api/Auth/change-password")]
        Task ChangePassword([Body] ChangePasswordRequest request);

        [Post("/api/Auth/register/user")]
        Task<AuthResult> RegisterUser([Body] RegisterUserRequest request);

        [Get("/api/Auth/me")]
        Task<object> Me();
    }

    public interface IAuthApiOpen
    {
        [Post("/api/Auth/login")]
        Task<AuthResult> Login([Body] LoginRequest request);

        [Post("/api/Auth/register/company")]
        Task<AuthResult> RegisterCompany([Body] RegisterCompanyRequest request);
    }
}
