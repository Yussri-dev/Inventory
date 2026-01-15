using Inventory.Dto.Auth.Requests;
using Inventory.Dto.Auth.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IAuthApi
    {
        [Post("/api/Auth/refresh")]
        Task<AuthResult> Refresh();

        [Post("/api/Auth/change-password")]
        Task ChangePassword(ChangePasswordRequest request);

        [Post("/api/Auth/register/user")]
        Task<AuthResult> RegisterUser([Body] RegisterUserRequest request);

        [Get("/api/Auth/me")]
        Task<object> Me();
    }


    public interface IAuthApiOpen
    {
        [Post("/api/Auth/login")]
        Task<AuthResult> Login(LoginRequest request);

        [Post("/api/Auth/register/company")]
        Task<AuthResult> RegisterCompany(RegisterCompanyRequest request);
    }
}
