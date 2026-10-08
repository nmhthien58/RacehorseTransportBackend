using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly ICacheService _cache;
        private readonly JwtOptions _jwtOptions;

        public AuthService(
            IRepository<User> userRepo,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            ICacheService cache,
            IOptions<JwtOptions> jwtOptions)
        {
            _userRepo = userRepo;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _cache = cache;
            _jwtOptions = jwtOptions.Value;
        }

        // Tự đăng ký chỉ tạo được tài khoản Customer; tài khoản nhân sự do Admin tạo.
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
            string cacheKey = CacheKeys.RefreshToken(_tokenService.HashRefreshToken(refreshToken));

            var session = await _cache.GetAsync<RefreshSession>(cacheKey);
            if (session == null)
            {
                throw new UnauthorizedException("Refresh token is invalid or has expired.");
            }

            // Mỗi refresh token chỉ dùng được một lần (rotation).
            await _cache.RemoveAsync(cacheKey);

            var user = await _userRepo.GetByIdAsync(session.UserId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedException("Refresh token is invalid or has expired.");
            }

            return await IssueTokensAsync(user);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            await _cache.RemoveAsync(CacheKeys.RefreshToken(_tokenService.HashRefreshToken(refreshToken)));
        }

        public async Task<UserModel> GetProfileAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} was not found.");
            }

            return user.ToBusinessModel();
        }

        // Người dùng tự sửa hồ sơ: chỉ họ tên và số điện thoại. Email và Role không đổi ở đây.
        public async Task<UserModel> UpdateProfileAsync(int userId, UpdateProfileModel model)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} was not found.");
            }

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

            await _userRepo.UpdateAsync(user);
            return user.ToBusinessModel();
        }

        public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} was not found.");
            }

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

        private async Task<AuthResultModel> IssueTokensAsync(User user)
        {
            var accessToken = _tokenService.CreateAccessToken(user);
            string refreshToken = _tokenService.CreateRefreshToken();

            await _cache.SetAsync(
                CacheKeys.RefreshToken(_tokenService.HashRefreshToken(refreshToken)),
                new RefreshSession { UserId = user.UserId },
                TimeSpan.FromDays(_jwtOptions.RefreshTokenDays));

            return new AuthResultModel
            {
                AccessToken = accessToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAt,
                RefreshToken = refreshToken,
                User = user.ToBusinessModel()
            };
        }

        private class RefreshSession
        {
            public int UserId { get; set; }
        }
    }
}
