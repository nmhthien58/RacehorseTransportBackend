using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    public class AuthService : IAuthService
    {
        // Khoảng cách tối thiểu giữa hai lần gửi lại email xác thực / đặt lại mật khẩu cho cùng một tài khoản
        private static readonly TimeSpan ResendInterval = TimeSpan.FromMinutes(1);

        private readonly IRepository<User> _userRepo;
        private readonly IRepository<UserToken> _tokenRepo;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly JwtOptions _jwtOptions;
        private readonly EmailOptions _emailOptions;

        public AuthService(
            IRepository<User> userRepo,
            IRepository<UserToken> tokenRepo,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IEmailService emailService,
            IOptions<JwtOptions> jwtOptions,
            IOptions<EmailOptions> emailOptions)
        {
            _userRepo = userRepo;
            _tokenRepo = tokenRepo;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _emailService = emailService;
            _jwtOptions = jwtOptions.Value;
            _emailOptions = emailOptions.Value;
        }

        // Tự đăng ký chỉ tạo được tài khoản Customer; tài khoản nhân sự do Admin tạo.
        // Tài khoản mới chưa xác thực email: vẫn đăng nhập được nhưng bị chặn ở các tính năng chính.
        public async Task<AuthResultModel> RegisterCustomerAsync(RegisterModel model)
        {
            string email = model.Email.Trim().ToLowerInvariant();

            if (await _userRepo.ExistsAsync(u => u.Email == email))
            {
                throw new BusinessException($"Email '{email}' is already registered.");
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = email,
                PasswordHash = _passwordHasher.Hash(model.Password),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                Role = UserRoles.Customer
            };

            await _userRepo.AddAsync(user);
            await SendVerificationEmailAsync(user);

            return await IssueTokensAsync(user);
        }

        public async Task<AuthResultModel> LoginAsync(string email, string password)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _userRepo.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            // Cùng một thông báo cho "sai email" và "sai mật khẩu" để không lộ email nào đã đăng ký.
            if (user == null || !_passwordHasher.Verify(password, user.PasswordHash))
            {
                throw new UnauthorizedException("Email or password is incorrect.");
            }

            if (!user.IsActive)
            {
                throw new ForbiddenException("This account has been deactivated.");
            }

            return await IssueTokensAsync(user);
        }

        public async Task<AuthResultModel> RefreshAsync(string refreshToken)
        {
            // Mỗi refresh token chỉ dùng được một lần (rotation).
            var token = await ConsumeTokenAsync(refreshToken, UserTokenTypes.Refresh);
            if (token == null)
            {
                throw new UnauthorizedException("Refresh token is invalid or has expired.");
            }

            var user = await _userRepo.GetByIdAsync(token.UserId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedException("Refresh token is invalid or has expired.");
            }

            return await IssueTokensAsync(user);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            string hash = _tokenService.HashRefreshToken(refreshToken);
            var token = await _tokenRepo.GetQueryable()
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.TokenType == UserTokenTypes.Refresh);

            if (token != null)
            {
                await _tokenRepo.DeleteAsync(token);
            }
        }

        public async Task<UserModel> GetProfileAsync(int userId)
        {
            var user = await GetUserAsync(userId);
            return user.ToBusinessModel();
        }

        // Người dùng tự sửa hồ sơ: chỉ họ tên và số điện thoại. Email và Role không đổi ở đây.
        public async Task<UserModel> UpdateProfileAsync(int userId, UpdateProfileModel model)
        {
            var user = await GetUserAsync(userId);

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

            await _userRepo.UpdateAsync(user);
            return user.ToBusinessModel();
        }

        public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await GetUserAsync(userId);

            if (!_passwordHasher.Verify(currentPassword, user.PasswordHash))
            {
                throw new BusinessException("Current password is incorrect.");
            }

            if (currentPassword == newPassword)
            {
                throw new BusinessException("New password must be different from the current password.");
            }

            user.PasswordHash = _passwordHasher.Hash(newPassword);
            await _userRepo.UpdateAsync(user);
        }

        // =============================================================================================
        // Xác thực email
        // =============================================================================================

        public async Task<UserModel> VerifyEmailAsync(string token)
        {
            var record = await ConsumeTokenAsync(token, UserTokenTypes.EmailVerification);
            if (record == null)
            {
                throw new BusinessException("The verification link is invalid or has expired. Please request a new one.");
            }

            var user = await GetUserAsync(record.UserId);
            if (user.EmailVerifiedAt == null)
            {
                user.EmailVerifiedAt = DateTimeOffset.UtcNow;
                await _userRepo.UpdateAsync(user);
            }

            return user.ToBusinessModel();
        }

        public async Task ResendVerificationAsync(int userId)
        {
            var user = await GetUserAsync(userId);

            if (user.EmailVerifiedAt != null)
            {
                throw new BusinessException("Your email address is already verified.");
            }

            await EnsureNotRequestedRecentlyAsync(userId, UserTokenTypes.EmailVerification);
            await SendVerificationEmailAsync(user);
        }

        public async Task<bool> IsEmailVerifiedAsync(int userId)
        {
            return await _userRepo.ExistsAsync(u => u.UserId == userId && u.EmailVerifiedAt != null);
        }

        // =============================================================================================
        // Quên mật khẩu
        // =============================================================================================

        // Luôn kết thúc êm, kể cả khi email không tồn tại, để không lộ email nào đã đăng ký.
        public async Task ForgotPasswordAsync(string email)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _userRepo.GetQueryable().FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user == null || !user.IsActive)
            {
                return;
            }

            // Bấm liên tục thì bỏ qua các lần sau, không báo lỗi
            if (await WasRequestedRecentlyAsync(user.UserId, UserTokenTypes.PasswordReset))
            {
                return;
            }

            int minutes = Math.Max(5, _emailOptions.PasswordResetTokenMinutes);
            string token = await CreateTokenAsync(user.UserId, UserTokenTypes.PasswordReset, TimeSpan.FromMinutes(minutes));

            _emailService.Queue(user.Email, user.FullName, EmailTemplateNames.ResetPassword, new Dictionary<string, string>
            {
                ["FullName"] = user.FullName,
                ["Link"] = FrontendLink("reset-password", token),
                ["ExpiresInMinutes"] = minutes.ToString()
            });
        }

        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            var record = await ConsumeTokenAsync(token, UserTokenTypes.PasswordReset);
            if (record == null)
            {
                throw new BusinessException("The password reset link is invalid or has expired. Please request a new one.");
            }

            var user = await GetUserAsync(record.UserId);
            if (!user.IsActive)
            {
                throw new ForbiddenException("This account has been deactivated.");
            }

            user.PasswordHash = _passwordHasher.Hash(newPassword);
            user.EmailVerifiedAt ??= DateTimeOffset.UtcNow; // mở được link trong hộp thư nghĩa là email đúng của người này
            await _userRepo.UpdateAsync(user);

            // Đăng xuất mọi thiết bị: refresh token cũ không dùng được nữa
            var sessions = await _tokenRepo.GetQueryable()
                .Where(t => t.UserId == user.UserId && t.TokenType == UserTokenTypes.Refresh)
                .ToListAsync();
            await _tokenRepo.DeleteRangeAsync(sessions);
        }

        // ---------------------------------------------------------------------------------------------

        private async Task<User> GetUserAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} was not found.");
            }
            return user;
        }

        private async Task SendVerificationEmailAsync(User user)
        {
            int hours = Math.Max(1, _emailOptions.VerificationTokenHours);
            string token = await CreateTokenAsync(user.UserId, UserTokenTypes.EmailVerification, TimeSpan.FromHours(hours));

            _emailService.Queue(user.Email, user.FullName, EmailTemplateNames.VerifyEmail, new Dictionary<string, string>
            {
                ["FullName"] = user.FullName,
                ["Link"] = FrontendLink("verify-email", token),
                ["ExpiresInHours"] = hours.ToString()
            });
        }

        // Link mở trang của frontend; trang đó đọc token trên URL rồi gọi API tương ứng
        private string FrontendLink(string page, string token)
        {
            return $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/{page}?token={Uri.EscapeDataString(token)}";
        }

        private async Task<AuthResultModel> IssueTokensAsync(User user)
        {
            var accessToken = _tokenService.CreateAccessToken(user);

            // Dọn các token đã dùng hoặc hết hạn của người này để bảng không phình ra
            var now = DateTimeOffset.UtcNow;
            var stale = await _tokenRepo.GetQueryable()
                .Where(t => t.UserId == user.UserId && (t.UsedAt != null || t.ExpiresAt <= now))
                .ToListAsync();
            if (stale.Count > 0)
            {
                await _tokenRepo.DeleteRangeAsync(stale);
            }

            string refreshToken = await CreateTokenAsync(user.UserId, UserTokenTypes.Refresh, TimeSpan.FromDays(_jwtOptions.RefreshTokenDays));

            return new AuthResultModel
            {
                AccessToken = accessToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAt,
                RefreshToken = refreshToken,
                User = user.ToBusinessModel()
            };
        }

        // Tạo token ngẫu nhiên, lưu mã băm, trả về chuỗi gốc để gửi cho người dùng.
        // Với link xác thực / đặt lại mật khẩu: link mới thay thế các link cũ chưa dùng.
        private async Task<string> CreateTokenAsync(int userId, string tokenType, TimeSpan lifetime)
        {
            if (tokenType != UserTokenTypes.Refresh)
            {
                var previous = await _tokenRepo.GetQueryable()
                    .Where(t => t.UserId == userId && t.TokenType == tokenType && t.UsedAt == null)
                    .ToListAsync();
                foreach (var old in previous)
                {
                    old.UsedAt = DateTimeOffset.UtcNow;
                }
            }

            string token = _tokenService.CreateRefreshToken();
            await _tokenRepo.AddAsync(new UserToken
            {
                UserId = userId,
                TokenType = tokenType,
                TokenHash = _tokenService.HashRefreshToken(token),
                ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime)
            });

            return token;
        }

        // Token hợp lệ (đúng loại, chưa dùng, chưa hết hạn) thì đánh dấu đã dùng và trả về; ngược lại trả null.
        private async Task<UserToken?> ConsumeTokenAsync(string token, string tokenType)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            string hash = _tokenService.HashRefreshToken(token.Trim());
            var now = DateTimeOffset.UtcNow;

            var record = await _tokenRepo.GetQueryable()
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.TokenType == tokenType && t.UsedAt == null && t.ExpiresAt > now);
            if (record == null)
            {
                return null;
            }

            record.UsedAt = now;
            await _tokenRepo.UpdateAsync(record);
            return record;
        }

        private async Task<bool> WasRequestedRecentlyAsync(int userId, string tokenType)
        {
            var since = DateTimeOffset.UtcNow.Subtract(ResendInterval);
            return await _tokenRepo.ExistsAsync(t => t.UserId == userId && t.TokenType == tokenType && t.CreatedAt > since);
        }

        private async Task EnsureNotRequestedRecentlyAsync(int userId, string tokenType)
        {
            if (await WasRequestedRecentlyAsync(userId, tokenType))
            {
                throw new BusinessException("An email was sent less than a minute ago. Please check your inbox, or try again in a minute.");
            }
        }
    }
}
