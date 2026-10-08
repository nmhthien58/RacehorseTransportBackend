using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Register a new Customer account.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterCustomerAsync(request.ToBusinessModel());

            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<AuthResponse>.SuccessResult(result.ToResponse(), "Account registered successfully"));
        }

        /// <summary>
        /// Log in with email and password.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);
            return Ok(ApiResponse<AuthResponse>.SuccessResult(result.ToResponse(), "Logged in successfully"));
        }

        /// <summary>
        /// Exchange a refresh token for a new access token (the old refresh token stops working).
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshAsync(request.RefreshToken);
            return Ok(ApiResponse<AuthResponse>.SuccessResult(result.ToResponse(), "Token refreshed successfully"));
        }

        /// <summary>
        /// Log out: revoke the refresh token.
        /// </summary>
        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Logged out successfully"));
        }

        /// <summary>
        /// Get the profile of the currently logged-in user.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            var user = await _authService.GetProfileAsync(User.GetUserId());
            return Ok(ApiResponse<UserResponse>.SuccessResult(user.ToResponse()));
        }

        /// <summary>
        /// Update own profile (full name, phone number).
        /// </summary>
        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
        {
            var user = await _authService.UpdateProfileAsync(User.GetUserId(), request.ToBusinessModel());
            return Ok(ApiResponse<UserResponse>.SuccessResult(user.ToResponse(), "Profile updated successfully"));
        }

        /// <summary>
        /// Change own password (requires the current password).
        /// </summary>
        [HttpPut("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            await _authService.ChangePasswordAsync(User.GetUserId(), request.CurrentPassword, request.NewPassword);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Password changed successfully"));
        }

        /// <summary>
        /// Confirm an email address with the token from the verification email.
        /// </summary>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            var user = await _authService.VerifyEmailAsync(request.Token);
            return Ok(ApiResponse<UserResponse>.SuccessResult(user.ToResponse(), "Email verified successfully"));
        }

        /// <summary>
        /// Send the verification email again to the logged-in user (at most once a minute).
        /// </summary>
        [HttpPost("resend-verification")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendVerification()
        {
            await _authService.ResendVerificationAsync(User.GetUserId());
            return Ok(ApiResponse<object?>.SuccessResult(null, "Verification email sent"));
        }

        /// <summary>
        /// Request a password reset email. Always answers 200, whether or not the email is registered.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.ForgotPasswordAsync(request.Email);
            return Ok(ApiResponse<object?>.SuccessResult(null, "If this email is registered, a password reset link has been sent."));
        }

        /// <summary>
        /// Set a new password with the token from the password reset email. All existing sessions are signed out.
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Password reset successfully"));
        }
    }
}
