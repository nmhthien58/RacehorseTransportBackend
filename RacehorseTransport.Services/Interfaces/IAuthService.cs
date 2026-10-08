using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultModel> RegisterCustomerAsync(RegisterModel model);
        Task<AuthResultModel> LoginAsync(string email, string password);
        Task<AuthResultModel> RefreshAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<UserModel> GetProfileAsync(int userId);
        Task<UserModel> UpdateProfileAsync(int userId, UpdateProfileModel model);
        Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
