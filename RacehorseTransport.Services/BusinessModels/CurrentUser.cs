using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Services.BusinessModels
{
    // Người đang gọi API (lấy từ access token). Controller tạo bằng User.ToCurrentUser() rồi truyền xuống service,
    // để service kiểm tra quyền trên từng bản ghi (ví dụ khách chỉ được sửa ngựa của mình).
    public class CurrentUser
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;

        public bool IsCustomer => Role == UserRoles.Customer;
        public bool IsStaff => !IsCustomer;

        public bool IsInRole(params string[] roles) => roles.Contains(Role);
    }
}
