using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultModel> RegisterCustomerAsync(RegisterModel model);
        Task<AuthResultModel> LoginAsync(string email, string password);
        Task<AuthResultModel> LoginWithGoogleAsync(string idToken);
        Task<AuthResultModel> RefreshAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<UserModel> GetProfileAsync(int userId);
        Task<UserModel> UpdateProfileAsync(int userId, UpdateProfileModel model);
        Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        // Xác thực email
        Task<UserModel> VerifyEmailAsync(string token);
        Task ResendVerificationAsync(int userId);
        Task<bool> IsEmailVerifiedAsync(int userId);

        // Quên mật khẩu
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(string token, string newPassword);
    }
}
