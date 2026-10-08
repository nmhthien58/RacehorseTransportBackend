using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Quản trị tài khoản (Admin) và danh sách nhân sự để chọn khi phân công
    public interface IUserService
    {
        Task<PagedBusinessResult<UserModel>> GetUsersAsync(UserFilterOptions options);
        Task<UserModel> GetUserByIdAsync(int id);
        Task<UserModel> CreateStaffAsync(CreateStaffModel model);
        Task<UserModel> UpdateUserAsync(int id, UpdateUserModel model, int currentUserId);
        Task<UserModel> SetActiveAsync(int id, bool isActive, int currentUserId);
        Task ResetPasswordAsync(int id, string newPassword);
        Task<IReadOnlyList<UserModel>> GetActiveStaffAsync(string? role);
    }
}
