using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Extensions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;

namespace RacehorseTransport.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IRepository<User> userRepo, IPasswordHasher passwordHasher)
        {
            _userRepo = userRepo;
            _passwordHasher = passwordHasher;
        }

        public async Task<PagedBusinessResult<UserModel>> GetUsersAsync(UserFilterOptions options)
        {
            var query = _userRepo.GetQueryable().AsNoTracking();

            // Filtering
            if (!string.IsNullOrWhiteSpace(options.Role))
            {
                ValidateRole(options.Role);
                query = query.Where(u => u.Role == options.Role);
            }

            if (options.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == options.IsActive.Value);
            }

            // Searching
            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                string search = options.Search.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(search)
                    || u.Email.ToLower().Contains(search)
                    || (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            // Sorting (không cho sắp theo PasswordHash)
            if (options.Sort != null && options.Sort.Contains(nameof(User.PasswordHash), StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException("Unknown sort field 'passwordHash'.");
            }
            query = query.ApplySort(options.Sort, nameof(User.UserId));

            // Paging
            return await query.ToPagedResultAsync(options, u => u.ToBusinessModel());
        }

        public async Task<UserModel> GetUserByIdAsync(int id)
        {
            var user = await GetExistingAsync(id);
            return user.ToBusinessModel();
        }

        // Admin tạo tài khoản nhân sự. Tài khoản Customer do khách tự đăng ký (POST /api/auth/register).
        public async Task<UserModel> CreateStaffAsync(CreateStaffModel model)
        {
            ValidateRole(model.Role);
            if (model.Role == UserRoles.Customer)
            {
                throw new BusinessException("Customer accounts are created through self-registration, not by an administrator.");
            }

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
                PhoneNumber = NormalizePhone(model.PhoneNumber),
                Role = model.Role
            };

            await _userRepo.AddAsync(user);
            return user.ToBusinessModel();
        }

        public async Task<UserModel> UpdateUserAsync(int id, UpdateUserModel model, int currentUserId)
        {
            var user = await GetExistingAsync(id);
            ValidateRole(model.Role);

            if (model.Role != user.Role)
            {
                if (id == currentUserId)
                {
                    throw new BusinessException("You cannot change your own role.");
                }

                // Khách hàng có ngựa và đơn gắn với tài khoản; nhân sự có việc đang được giao.
                // Đổi qua lại giữa hai nhóm sẽ làm dữ liệu đó mồ côi.
                if (user.Role == UserRoles.Customer || model.Role == UserRoles.Customer)
                {
                    throw new BusinessException("An account cannot be switched between Customer and a staff role.");
                }
            }

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = NormalizePhone(model.PhoneNumber);
            user.Role = model.Role;

            await _userRepo.UpdateAsync(user);
            return user.ToBusinessModel();
        }

        public async Task<UserModel> SetActiveAsync(int id, bool isActive, int currentUserId)
        {
            var user = await GetExistingAsync(id);

            if (!isActive && id == currentUserId)
            {
                throw new BusinessException("You cannot deactivate your own account.");
            }

            user.IsActive = isActive;
            await _userRepo.UpdateAsync(user);
            return user.ToBusinessModel();
        }

        public async Task ResetPasswordAsync(int id, string newPassword)
        {
            var user = await GetExistingAsync(id);

            user.PasswordHash = _passwordHasher.Hash(newPassword);
            await _userRepo.UpdateAsync(user);
        }

        // Danh sách nhân sự đang hoạt động, dùng cho dropdown chọn người (Specialist khi duyệt đơn, tài xế khi lập chuyến)
        public async Task<IReadOnlyList<UserModel>> GetActiveStaffAsync(string? role)
        {
            var query = _userRepo.GetQueryable().AsNoTracking()
                .Where(u => u.IsActive && u.Role != UserRoles.Customer);

            if (!string.IsNullOrWhiteSpace(role))
            {
                ValidateRole(role);
                query = query.Where(u => u.Role == role);
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            return users.Select(u => u.ToBusinessModel()).ToList();
        }

        private async Task<User> GetExistingAsync(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {id} was not found.");
            }
            return user;
        }

        private static void ValidateRole(string role)
        {
            if (!UserRoles.All.Contains(role))
            {
                throw new BusinessException($"Role '{role}' is invalid. Allowed roles: {string.Join(", ", UserRoles.All)}.");
            }
        }

        private static string? NormalizePhone(string? phoneNumber)
        {
            return string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        }
    }
}
