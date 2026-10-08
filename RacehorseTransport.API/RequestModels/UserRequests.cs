using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class UserQueryRequest : QueryOptionsRequest
    {
        [FromQuery(Name = "role")]
        public string? Role { get; set; }

        [FromQuery(Name = "isActive")]
        public bool? IsActive { get; set; }
    }

    public class CreateStaffRequest
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

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "FullName is required.")]
        [MaxLength(100, ErrorMessage = "FullName cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [RegularExpression(@"^$|^\+?[0-9 ().\-]{6,30}$", ErrorMessage = "PhoneNumber format is invalid.")] // cho phép để trống
        [MaxLength(30, ErrorMessage = "PhoneNumber cannot exceed 30 characters.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;
    }

    public class SetUserActiveRequest
    {
        [Required(ErrorMessage = "IsActive is required.")]
        public bool? IsActive { get; set; }
    }

    public class ResetPasswordByAdminRequest
    {
        [Required(ErrorMessage = "NewPassword is required.")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "NewPassword must be between 8 and 64 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    // Dùng ở AuthController: người dùng tự sửa hồ sơ / tự đổi mật khẩu
    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "FullName is required.")]
        [MaxLength(100, ErrorMessage = "FullName cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [RegularExpression(@"^$|^\+?[0-9 ().\-]{6,30}$", ErrorMessage = "PhoneNumber format is invalid.")] // cho phép để trống
        [MaxLength(30, ErrorMessage = "PhoneNumber cannot exceed 30 characters.")]
        public string? PhoneNumber { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "CurrentPassword is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "NewPassword is required.")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "NewPassword must be between 8 and 64 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
