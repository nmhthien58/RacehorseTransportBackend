using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "FullName is required.")]
        [MaxLength(100, ErrorMessage = "FullName cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 64 characters.")]
        public string Password { get; set; } = string.Empty;

        [RegularExpression(@"^$|^\+?[0-9 ().\-]{6,30}$", ErrorMessage = "PhoneNumber format is invalid.")] // cho phép để trống
        [MaxLength(30, ErrorMessage = "PhoneNumber cannot exceed 30 characters.")]
        public string? PhoneNumber { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }

    public class GoogleLoginRequest
    {
        // ID token (chuỗi "credential") nhận từ nút "Đăng nhập bằng Google" ở frontend
        [Required(ErrorMessage = "IdToken is required.")]
        public string IdToken { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "RefreshToken is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Token is required.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "NewPassword is required.")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "NewPassword must be between 8 and 64 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
